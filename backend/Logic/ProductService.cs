using backend.Data;

namespace backend.Logic;

public class ProductService
{
    private readonly ProductRepository _products;

    public ProductService(ProductRepository products)
    {
        _products = products;
    }

    public async Task<List<ProductDto>> GetAll()
    {
        return await _products.GetAll();
    }

    public async Task<ProductDto?> GetById(int id)
    {
        return await _products.GetById(id);
    }

    public async Task<ProductDto?> Add(ProductCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return null;

        if (request.BasePrice <= 0)
            return null;

        if (request.StockQuantity < 0)
            return null;

        if (request.CategoryId <= 0)
            return null;

        return await _products.Add(request);
    }

    public async Task<bool> Update(int id, ProductUpdateRequest request)
    {
        return await _products.Update(id, request);
    }

    public async Task<bool> Delete(int id)
    {
        return await _products.Delete(id);
    }

    public async Task<bool> SetStock(int productId, int quantity)
    {
        if (quantity < 0)
            return false;

        return await _products.SetStock(productId, quantity);
    }

    public async Task<bool> AddStock(int productId, int amount)
    {
        if (amount <= 0)
            return false;

        var currentStock = await _products.GetStock(productId);

        if (currentStock == null)
            return false;

        return await _products.SetStock(productId, currentStock.Value + amount);
    }

    public async Task<bool> ReduceStock(int productId, int amount)
    {
        if (amount <= 0)
            return false;

        var currentStock = await _products.GetStock(productId);

        if (currentStock == null)
            return false;

        if (currentStock.Value < amount)
            return false;

        return await _products.SetStock(productId, currentStock.Value - amount);
    }
}
