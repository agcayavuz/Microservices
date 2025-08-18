namespace CatalogService.Domain.Entities
{
    public sealed class Product
    {
        public int Id { get; init; }
        public string Name { get; init; } = default!;
        public string? Description { get; init; }
        public decimal Price { get; init; }
        public int CategoryId { get; init; }
        public int BrandId { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
