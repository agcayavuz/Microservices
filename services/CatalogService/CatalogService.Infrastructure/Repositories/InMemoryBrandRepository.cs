using CatalogService.Application.Abstractions;
using CatalogService.Domain.Entities;

namespace CatalogService.Infrastructure.Repositories
{

    public sealed class InMemoryBrandRepository : IBrandRepository
    {
        private static readonly IReadOnlyList<Brand> Seed =
        [
            new Brand { Id = 100, Name = "ForgePro" },
        new Brand { Id = 101, Name = "SafeSight" },
        new Brand { Id = 102, Name = "DrillMaster" }
        ];

        public Task<IReadOnlyList<Brand>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(Seed);
    }
}
