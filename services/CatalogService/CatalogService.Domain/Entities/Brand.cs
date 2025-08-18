namespace CatalogService.Domain.Entities
{
    public sealed class Brand
    {
        public int Id { get; init; }
        public string Name { get; init; } = default!;
    }
}
