namespace CatalogService.Application.Products
{
    public sealed class ProductResponse
    {
        public int Id { get; init; }
        public string Name { get; init; } = default!;
        public string? Description { get; init; }
        public decimal Price { get; init; }
        public int CategoryId { get; init; }
        public int BrandId { get; init; }
    }
}
