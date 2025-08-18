namespace CatalogService.Application.Categories
{
    public interface ICategoryService
    {
        Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken ct = default);
    }
}
