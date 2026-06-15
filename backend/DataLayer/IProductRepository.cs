namespace backend.Data;

public interface IProductRepository
{
    Task<List<ProductDto>> GetAll();
    Task<ProductDto?> GetById(int id);
    Task<ProductDto?> Add(ProductCreateRequest request);
    Task<bool> Update(int id, ProductUpdateRequest request);
    Task<bool> Delete(int id);
    Task<int?> GetStock(int productId);
    Task<bool> SetStock(int productId, int quantity);
    Task<bool> SetImage(int productId, string imageUrl);
}
