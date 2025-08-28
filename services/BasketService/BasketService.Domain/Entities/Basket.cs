namespace BasketService.Domain.Entities
{

    public sealed class Basket
    {
        public string CustomerId { get; }
        private readonly List<BasketItem> _items = new();

        public IReadOnlyCollection<BasketItem> Items => _items.AsReadOnly();

        public Basket(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId)) throw new ArgumentException("CustomerId boş olamaz");
            CustomerId = customerId.Trim();
        }

        public void ReplaceItems(IEnumerable<BasketItem> items)
        {
            _items.Clear();
            _items.AddRange(items);
        }

        public void UpsertItem(BasketItem item)
        {
            var existing = _items.FirstOrDefault(x => x.ProductId == item.ProductId);
            if (existing is null)
            {
                _items.Add(item);
            }
            else
            {
                // Ürün adı ve fiyatı güncellenebilir; miktar item.Quantity olarak set edilir
                existing.SetQuantity(item.Quantity);
            }
        }

        /// <summary>
        /// Aynı ürünü tekrar ekleme durumunda toplama (increment) davranışı.
        /// Ürün yoksa yeni ekler; varsa miktarı artırır.
        /// </summary>
        public void AddOrIncreaseItem(BasketItem item)
        {
            var existing = _items.FirstOrDefault(x => x.ProductId == item.ProductId);
            if (existing is null)
                _items.Add(item);
            else
                existing.Increase(item.Quantity);
        }

        public bool RemoveItem(string productId)
        {
            var item = _items.FirstOrDefault(x => x.ProductId == productId);
            if (item is null) return false;
            _items.Remove(item);
            return true;
        }

        public bool IncreaseItemQuantity(string productId, int qty)
        {
            var item = _items.FirstOrDefault(x => x.ProductId == productId);
            if (item is null) return false;
            item.Increase(qty);
            return true;
        }

        public bool DecreaseItemQuantity(string productId, int qty)
        {
            var item = _items.FirstOrDefault(x => x.ProductId == productId);
            if (item is null) return false;

            item.Decrease(qty);

            if (item.Quantity == 0)
                _items.Remove(item);

            return true;
        }

        public decimal TotalAmount => _items.Sum(i => i.LineTotal);

        public bool IsEmpty => !_items.Any();

    }
}
