using StackExchange.Redis;
using System.Text.Json;

namespace backend.Logic;

public class RedisProductCache : IProductCache
{
    private const string ProductsCacheKey = "products:all";
    private static readonly TimeSpan ProductsCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _cache;

    public RedisProductCache(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _cache = redis.GetDatabase();
    }

    public async Task<List<ProductDto>?> GetProducts()
    {
        if (!_redis.IsConnected)
            return null;

        try
        {
            var cachedProducts = await _cache.StringGetAsync(ProductsCacheKey);
            if (!cachedProducts.HasValue)
                return null;

            return JsonSerializer.Deserialize<List<ProductDto>>(
                cachedProducts.ToString(),
                JsonOptions);
        }
        catch (RedisException ex)
        {
            Console.WriteLine($"[REDIS] Product cache read skipped: {ex.Message}");
            return null;
        }
    }

    public async Task SetProducts(List<ProductDto> products)
    {
        if (!_redis.IsConnected)
            return;

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

    public async Task ClearProducts()
    {
        if (!_redis.IsConnected)
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
}
