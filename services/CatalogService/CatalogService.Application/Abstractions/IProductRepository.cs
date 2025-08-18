using CatalogService.Domain.Entities;

namespace CatalogService.Application.Abstractions
{
    public interface IProductRepository
    {
        Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    }
}
