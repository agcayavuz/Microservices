namespace CatalogService.Application.Brands
{
    public interface IBrandService
    {
        Task<IReadOnlyList<BrandResponse>> GetAllAsync(CancellationToken ct = default);
    }
}
