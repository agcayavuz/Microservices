namespace CatalogService.Domain.Entities
{
    public sealed class Category
    {
        public int Id { get; init; }
        public string Name { get; init; } = default!;
    }
}
