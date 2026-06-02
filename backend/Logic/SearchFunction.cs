using backend.Data;

public class SearchFunction
{
    private readonly ProductRepository _productRepository;

    public SearchFunction(ProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<List<ProductDto>> Search(string? searchedProduct, int? categoryId, string? brand,
        decimal? minPrice, decimal? maxPrice, string? colorName, string? sizeName)
    {
        var products = await _productRepository.GetAll();

        var query = products.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(searchedProduct))
        {
            query = query.Where(p =>
                p.Name.Contains(searchedProduct, StringComparison.OrdinalIgnoreCase) ||
                (p.Description != null &&
                 p.Description.Contains(searchedProduct, StringComparison.OrdinalIgnoreCase)) ||
                p.Brand.Contains(searchedProduct, StringComparison.OrdinalIgnoreCase));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(brand))
        {
            query = query.Where(p =>
                p.Brand.Equals(brand, StringComparison.OrdinalIgnoreCase));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice <= maxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(colorName))
        {
            query = query.Where(p =>
                p.ColorName != null &&
                p.ColorName.Equals(colorName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(sizeName))
        {
            query = query.Where(p =>
                p.SizeName != null &&
                p.SizeName.Equals(sizeName, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToList();
    }
}