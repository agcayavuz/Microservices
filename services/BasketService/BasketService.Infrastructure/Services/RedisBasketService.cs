using BasketService.Infrastructure.Redis;
using BasketService.Application.Interfaces;
using BasketService.Application.Options;
using BasketService.Contracts.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace BasketService.Infrastructure.Services
{
    public sealed class RedisBasketService : IBasketService
    {
        private readonly IDatabase _db;
        private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
        private readonly IOptionsMonitor<BasketOptions> _basketOpt;
        private readonly string _keyPrefix;
        private readonly TimeSpan _ttl;
        private readonly TimeSpan _lockExpiry;
        private readonly TimeSpan _lockRetry;
        private readonly TimeSpan _lockMaxWait;
        private string Key(string customerId) => $"{_keyPrefix}:{customerId}";
        private string LockKey(string customerId) => $"{_keyPrefix}:{customerId}:lock";


        public RedisBasketService(IConnectionMultiplexer mux, IOptionsMonitor<BasketOptions> basketOpt, IConfiguration cfg)
        {
            _db = mux.GetDatabase();
            _basketOpt = basketOpt;
            _keyPrefix = cfg.GetValue<string>("Redis:KeyPrefix") ?? "basket";
            var days = Math.Max(1, cfg.GetValue<int?>("Redis:DefaultTtlDays") ?? 30);
            _ttl = TimeSpan.FromDays(days);

            var sec = Math.Max(1, cfg.GetValue<int?>("Redis:Lock:ExpirySeconds") ?? 5);
            var rMs = Math.Max(1, cfg.GetValue<int?>("Redis:Lock:RetryMs") ?? 50);
            var wMs = Math.Max(100, cfg.GetValue<int?>("Redis:Lock:MaxWaitMs") ?? 1000);
            _lockExpiry = TimeSpan.FromSeconds(sec);
            _lockRetry = TimeSpan.FromMilliseconds(rMs);
            _lockMaxWait = TimeSpan.FromMilliseconds(wMs);
        }       

        public async Task<BasketDto?> GetAsync(string customerId, CancellationToken ct = default)
        {
            var val = await _db.StringGetAsync(Key(customerId));
            if (val.IsNullOrEmpty) return null;

            var basket = JsonSerializer.Deserialize<BasketDto>(val!, _json);
            return basket;
        }

        public async Task<BasketDto> CreateOrReplaceAsync(string customerId, IReadOnlyCollection<BasketItemDto> items, CancellationToken ct = default)
        {
            var mode = _basketOpt.CurrentValue.CreateOrReplaceBehavior;

            if (mode == enumCreateOrReplaceMode.Replace)
            {
                var replaced = new BasketDto
                {
                    CustomerId = customerId,
                    Items = items.ToList(),
                    TotalAmount = items.Sum(i => i.UnitPrice * i.Quantity)
                };
                await _db.StringSetAsync(Key(customerId), JsonSerializer.Serialize(replaced, _json), _ttl, When.Always);
                return replaced;
            }

            // MERGE: atomik hale getir
            await using var theLock = await DistributedLock.AcquireAsync(_db, LockKey(customerId), _lockExpiry, _lockRetry, _lockMaxWait)
                                  ?? throw new InvalidOperationException("basket_locked");

            var current = await GetAsync(customerId, ct) ?? new BasketDto { CustomerId = customerId, Items = new List<BasketItemDto>() };
            var list = current.Items.ToList();

            foreach (var incoming in items)
            {
                var existing = list.FirstOrDefault(x => x.ProductId == incoming.ProductId);
                if (existing is null)
                {
                    list.Add(new BasketItemDto
                    {
                        ProductId = incoming.ProductId,
                        ProductName = incoming.ProductName,
                        UnitPrice = incoming.UnitPrice,
                        Quantity = incoming.Quantity
                    });
                }
                else
                {
                    var idx = list.IndexOf(existing);
                    list[idx] = new BasketItemDto
                    {
                        ProductId = existing.ProductId,
                        ProductName = existing.ProductName,
                        UnitPrice = existing.UnitPrice,
                        Quantity = existing.Quantity + incoming.Quantity
                    };
                }
            }

            var merged = new BasketDto
            {
                CustomerId = customerId,
                Items = list,
                TotalAmount = list.Sum(i => i.UnitPrice * i.Quantity)
            };

            await _db.StringSetAsync(Key(customerId), JsonSerializer.Serialize(merged, _json), _ttl, When.Always);
            return merged;
        }


        public async Task<BasketDto> UpsertItemsAsync(string customerId, IReadOnlyCollection<BasketItemDto> items, CancellationToken ct = default)
        {
            // OVERWRITE/SET (idempotent)
            var dto = new BasketDto
            {
                CustomerId = customerId,
                Items = items.ToList(),
                TotalAmount = items.Sum(i => i.UnitPrice * i.Quantity)
            };

            await _db.StringSetAsync(Key(customerId), JsonSerializer.Serialize(dto, _json), _ttl, When.Always);
            return dto;
        }

        public async Task<bool> IncreaseItemQuantityAsync(string customerId, string productId, int qty, CancellationToken ct = default)
        {
            if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
            await using var theLock = await DistributedLock.AcquireAsync(_db, LockKey(customerId), _lockExpiry, _lockRetry, _lockMaxWait)
                                  ?? throw new InvalidOperationException("basket_locked");

            var basket = await GetAsync(customerId, ct);
            if (basket is null) return false;

            var items = basket.Items.ToList();
            var existing = items.FirstOrDefault(i => i.ProductId == productId);
            if (existing is null) return false;

            var idx = items.IndexOf(existing);
            items[idx] = new BasketItemDto
            {
                ProductId = existing.ProductId,
                ProductName = existing.ProductName,
                UnitPrice = existing.UnitPrice,
                Quantity = existing.Quantity + qty
            };

            var dto = new BasketDto
            {
                CustomerId = customerId,
                Items = items,
                TotalAmount = items.Sum(i => i.UnitPrice * i.Quantity)
            };

            await _db.StringSetAsync(Key(customerId), JsonSerializer.Serialize(dto, _json), _ttl, When.Always);
            return true;
        }

        public async Task<bool> DecreaseItemQuantityAsync(string customerId, string productId, int qty, CancellationToken ct = default)
        {
            if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
            await using var theLock = await DistributedLock.AcquireAsync(_db, LockKey(customerId), _lockExpiry, _lockRetry, _lockMaxWait)
                                  ?? throw new InvalidOperationException("basket_locked");

            var basket = await GetAsync(customerId, ct);
            if (basket is null) return false;

            var items = basket.Items.ToList();
            var existing = items.FirstOrDefault(i => i.ProductId == productId);
            if (existing is null) return false;

            var newQty = existing.Quantity - qty;
            if (newQty <= 0)
                items.Remove(existing);
            else
            {
                var idx = items.IndexOf(existing);
                items[idx] = new BasketItemDto
                {
                    ProductId = existing.ProductId,
                    ProductName = existing.ProductName,
                    UnitPrice = existing.UnitPrice,
                    Quantity = newQty
                };
            }

            if (!items.Any() && _basketOpt.CurrentValue.AutoDeleteEmptyOnItemRemove)
            {
                await _db.KeyDeleteAsync(Key(customerId));
                return true;
            }

            var dto = new BasketDto
            {
                CustomerId = customerId,
                Items = items,
                TotalAmount = items.Sum(i => i.UnitPrice * i.Quantity)
            };
            await _db.StringSetAsync(Key(customerId), JsonSerializer.Serialize(dto, _json), _ttl, When.Always);
            return true;
        }

        public async Task<bool> RemoveItemAsync(string customerId, string productId, CancellationToken ct = default)
        {
            await using var theLock = await DistributedLock.AcquireAsync(_db, LockKey(customerId), _lockExpiry, _lockRetry, _lockMaxWait)
                                  ?? throw new InvalidOperationException("basket_locked");

            var basket = await GetAsync(customerId, ct);
            if (basket is null) return false;

            var items = basket.Items.Where(i => i.ProductId != productId).ToList();

            if (!items.Any() && _basketOpt.CurrentValue.AutoDeleteEmptyOnItemRemove)
            {
                await _db.KeyDeleteAsync(Key(customerId));
                return true;
            }

            if (items.Count == basket.Items.Count) return false;

            var dto = new BasketDto
            {
                CustomerId = customerId,
                Items = items,
                TotalAmount = items.Sum(i => i.UnitPrice * i.Quantity)
            };
            await _db.StringSetAsync(Key(customerId), JsonSerializer.Serialize(dto, _json), _ttl, When.Always);
            return true;
        }


        public async Task<bool> DeleteAsync(string customerId, CancellationToken ct = default)
            => await _db.KeyDeleteAsync(Key(customerId));
    }
}
