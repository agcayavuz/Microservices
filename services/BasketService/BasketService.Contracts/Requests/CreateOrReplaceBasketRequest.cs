using BasketService.Contracts.Dtos;

namespace BasketService.Contracts.Requests
{
    public sealed class CreateOrReplaceBasketRequest
    {
        public string CustomerId { get; init; } = default!;
        public IReadOnlyCollection<BasketItemDto> Items { get; init; } = new List<BasketItemDto>();
    }
}
