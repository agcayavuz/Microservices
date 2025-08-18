using CatalogService.Application.Abstractions;

namespace CatalogService.Application.Brands
{
    public sealed class BrandService(IBrandRepository repo) : IBrandService
    {
        public async Task<IReadOnlyList<BrandResponse>> GetAllAsync(CancellationToken ct = default)
            => (await repo.GetAllAsync(ct))
                .Select(b => new BrandResponse { Id = b.Id, Name = b.Name })
                .ToList();
    }
}
