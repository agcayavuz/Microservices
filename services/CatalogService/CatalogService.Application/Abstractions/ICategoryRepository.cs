using CatalogService.Domain.Entities;

namespace CatalogService.Application.Abstractions
{
    public interface ICategoryRepository
    {
        Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
    }
}
