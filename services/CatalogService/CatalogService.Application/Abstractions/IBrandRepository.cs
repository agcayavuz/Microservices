using CatalogService.Domain.Entities;

namespace CatalogService.Application.Abstractions
{
    public interface IBrandRepository
    {
        Task<IReadOnlyList<Brand>> GetAllAsync(CancellationToken ct = default);
    }
}
