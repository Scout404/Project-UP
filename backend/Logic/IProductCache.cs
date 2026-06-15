namespace backend.Logic;

public interface IProductCache
{
    Task<List<ProductDto>?> GetProducts();
    Task SetProducts(List<ProductDto> products);
    Task ClearProducts();
}
