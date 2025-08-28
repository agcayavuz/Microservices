namespace BasketService.Contracts.Requests
{
    public sealed class UpsertBasketItemRequest
    {
        public string ProductId { get; init; } = default!;
        public string ProductName { get; init; } = string.Empty;
        public decimal UnitPrice { get; init; }
        public int Quantity { get; init; }
    }
}
