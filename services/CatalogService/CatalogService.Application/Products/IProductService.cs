namespace CatalogService.Application.Products
{
    public interface IProductService
    {
        Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken ct = default);
    }
}
