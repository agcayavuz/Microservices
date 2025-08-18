using CatalogService.Application.Abstractions;
using CatalogService.Domain.Entities;

namespace CatalogService.Infrastructure.Repositories
{
    public sealed class InMemoryProductRepository : IProductRepository
    {
        private static readonly IReadOnlyList<Product> Seed =
        [
            new Product { Id = 1, Name = "Steel Hammer", Description = "16oz claw hammer", Price = 12.99m, CategoryId = 10, BrandId = 100, CreatedAt = DateTime.UtcNow },
        new Product { Id = 2, Name = "Safety Glasses", Description = "ANSI Z87.1", Price = 6.49m, CategoryId = 11, BrandId = 101, CreatedAt = DateTime.UtcNow },
        new Product { Id = 3, Name = "Cordless Drill", Description = "18V brushless", Price = 89.00m, CategoryId = 12, BrandId = 102, CreatedAt = DateTime.UtcNow }
        ];

        public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(Seed);
    }

}
