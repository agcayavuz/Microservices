using CatalogService.Application.Abstractions;
using CatalogService.Domain.Entities;

namespace CatalogService.Infrastructure.Repositories
{
    public sealed class InMemoryCategoryRepository : ICategoryRepository
    {
        private static readonly IReadOnlyList<Category> Seed =
        [
            new Category { Id = 10, Name = "Hand Tools" },
        new Category { Id = 11, Name = "Safety" },
        new Category { Id = 12, Name = "Power Tools" }
        ];

        public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(Seed);
    }

}
