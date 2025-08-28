using BasketService.Contracts.Dtos;
using System.Threading;

namespace BasketService.Application.Interfaces
{
    public interface IBasketService
    {
        Task<BasketDto?> GetAsync(string customerId, CancellationToken ct = default);
        Task<BasketDto> CreateOrReplaceAsync(string customerId, IReadOnlyCollection<BasketItemDto> items, CancellationToken ct = default);
        Task<BasketDto> UpsertItemsAsync(string customerId, IReadOnlyCollection<BasketItemDto> items, CancellationToken ct = default);

        Task<bool> IncreaseItemQuantityAsync(string customerId, string productId, int qty, CancellationToken ct = default);
        Task<bool> DecreaseItemQuantityAsync(string customerId, string productId, int qty, CancellationToken ct = default);

        Task<bool> RemoveItemAsync(string customerId, string productId, CancellationToken ct = default);
        Task<bool> DeleteAsync(string customerId, CancellationToken ct = default);
    }
}
