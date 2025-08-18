using CatalogService.Application.Abstractions;

namespace CatalogService.Application.Categories
{
    public sealed class CategoryService(ICategoryRepository repo) : ICategoryService
    {
        public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken ct = default)
            => (await repo.GetAllAsync(ct))
                .Select(c => new CategoryResponse { Id = c.Id, Name = c.Name })
                .ToList();
    }
}
