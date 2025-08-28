namespace BasketService.Contracts.Dtos
{
    public sealed class BasketDto
    {
        public string CustomerId { get; init; } = default!;
        public IReadOnlyCollection<BasketItemDto> Items { get; init; } = new List<BasketItemDto>();
        public decimal TotalAmount { get; init; }
    }
}
