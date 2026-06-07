using backend.Data;
using StackExchange.Redis;
using System.Text.Json;

namespace backend.Logic;

public class ProductService
{
    private const string ProductsCacheKey = "products:all";
    private static readonly TimeSpan ProductsCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ProductRepository _products;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _cache;

    public ProductService(ProductRepository products, IConnectionMultiplexer redis)
    {
        _products = products;
        _redis = redis;
        _cache = redis.GetDatabase();
    }

    public async Task<List<ProductDto>> GetAll()
    {
        if (IsRedisAvailable())
        {
            try
            {
                var cachedProducts = await _cache.StringGetAsync(ProductsCacheKey);
                if (cachedProducts.HasValue)
                {
                    var productsFromCache = JsonSerializer.Deserialize<List<ProductDto>>(
                        cachedProducts.ToString(),
                        JsonOptions);

                    if (productsFromCache != null)
                        return productsFromCache;
                }
            }
            catch (RedisException ex)
            {
                Console.WriteLine($"[REDIS] Product cache read skipped: {ex.Message}");
            }
        }

        var products = await _products.GetAll();

        if (IsRedisAvailable())
        {
            try
            {
                await _cache.StringSetAsync(
                    ProductsCacheKey,
                    JsonSerializer.Serialize(products, JsonOptions),
                    ProductsCacheDuration);
            }
            catch (RedisException ex)
            {
                Console.WriteLine($"[REDIS] Product cache write skipped: {ex.Message}");
            }
        }

        return products;
    }

    public async Task<ProductDto?> GetById(int id)
    {
        return await _products.GetById(id);
    }

    public async Task<ProductDto?> Add(ProductCreateRequest request)
    {
        if (request.BasePrice <= 0 || request.StockQuantity < 0)
            return null;

        var created = await _products.Add(new ProductCreateRequest
        {
            Name = string.IsNullOrWhiteSpace(request.Name)
                ? "Unnamed Product"
                : request.Name.Trim(),
            Description = request.Description?.Trim() ?? "",
            Brand = string.IsNullOrWhiteSpace(request.Brand)
                ? "Unknown"
                : request.Brand.Trim(),
            BasePrice = request.BasePrice,
            CategoryId = request.CategoryId,
            IsActive = request.IsActive,
            StockQuantity = request.StockQuantity,
            ColorName = request.ColorName?.Trim(),
            SizeName = request.SizeName?.Trim()
        });

        if (created == null)
            return null;

        await ClearProductsCache();

        return created;
    }

    public async Task<bool> Update(int id, ProductUpdateRequest request)
    {
        var existing = await _products.GetById(id);
        if (existing == null)
            return false;

        var updated = new ProductUpdateRequest
        {
            Name = string.IsNullOrWhiteSpace(request.Name)
                ? existing.Name
                : request.Name.Trim(),
            Description = request.Description ?? existing.Description,
            Brand = string.IsNullOrWhiteSpace(request.Brand)
                ? existing.Brand
                : request.Brand.Trim(),
            BasePrice = request.BasePrice ?? existing.BasePrice,
            CategoryId = request.CategoryId ?? existing.CategoryId,
            IsActive = request.IsActive ?? existing.IsActive,
            StockQuantity = request.StockQuantity ?? existing.StockQuantity,
            ColorName = string.IsNullOrWhiteSpace(request.ColorName)
                ? existing.ColorName
                : request.ColorName.Trim(),
            SizeName = string.IsNullOrWhiteSpace(request.SizeName)
                ? existing.SizeName
                : request.SizeName.Trim()
        };

        if (updated.BasePrice <= 0 || updated.StockQuantity < 0)
            return false;

        var success = await _products.Update(id, updated);
        if (!success)
            return false;

        await ClearProductsCache();
        return true;
    }

    public async Task<bool> Delete(int id)
    {
        var success = await _products.Delete(id);
        if (!success)
            return false;

        await ClearProductsCache();
        return true;
    }

    public async Task<bool> SetStock(int productId, int quantity)
    {
        if (quantity < 0)
            return false;

        var success = await _products.SetStock(productId, quantity);
        if (!success)
            return false;

        await ClearProductsCache();
        return true;
    }

    public async Task<bool> SetImage(int productId, string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return false;

        var success = await _products.SetImage(productId, imageUrl.Trim());
        if (!success)
            return false;

        await ClearProductsCache();
        return true;
    }

    public async Task<bool> AddStock(int productId, int amount)
    {
        if (amount <= 0)
            return false;

        var currentStock = await _products.GetStock(productId);

        if (currentStock == null)
            return false;

        var success = await _products.SetStock(productId, currentStock.Value + amount);
        if (!success)
            return false;

        await ClearProductsCache();
        return true;
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

        var success = await _products.SetStock(productId, currentStock.Value - amount);
        if (!success)
            return false;

        await ClearProductsCache();
        return true;
    }

    private async Task ClearProductsCache()
    {
        if (!IsRedisAvailable())
            return;

        try
        {
            await _cache.KeyDeleteAsync(ProductsCacheKey);
        }
        catch (RedisException ex)
        {
            Console.WriteLine($"[REDIS] Product cache clear skipped: {ex.Message}");
        }
    }

    private bool IsRedisAvailable()
    {
        return _redis.IsConnected;
    }
}
