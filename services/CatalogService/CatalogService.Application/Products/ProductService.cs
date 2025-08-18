using CatalogService.Application.Abstractions;

namespace CatalogService.Application.Products
{
    public sealed class ProductService(IProductRepository repository) : IProductService
    {
        public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken ct = default)
        {
            var items = await repository.GetAllAsync(ct);
            // Küçük, manuel map — ileride AutoMapper veya source generator’lı alternatifleri konuşacağız
            return items.Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId,
                BrandId = p.BrandId
            }).ToList();
        }
    }

}
