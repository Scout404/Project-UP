using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace backend.Logic;

public class ProductService
{
    private const string ProductsCacheKey = "products:all";
    private static readonly TimeSpan ProductsCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _context;
    private readonly IDatabase _cache;

    public ProductService(AppDbContext context, IConnectionMultiplexer redis)
    {
        _context = context;
        _cache = redis.GetDatabase();
    }

    public async Task<List<ProductDto>> GetAll()
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
            Console.WriteLine($"[REDIS] Product cache read failed: {ex.Message}");
        }

        var products = await _context.Products
            .Include(p => p.Category)
            .Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Brand = p.Brand,
                BasePrice = p.BasePrice,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : "",
                IsActive = p.IsActive,
                StockQuantity = p.StockQuantity
            })
            .ToListAsync();

        try
        {
            await _cache.StringSetAsync(
                ProductsCacheKey,
                JsonSerializer.Serialize(products, JsonOptions),
                ProductsCacheDuration);
        }
        catch (RedisException ex)
        {
            Console.WriteLine($"[REDIS] Product cache write failed: {ex.Message}");
        }

        return products;
    }

    public async Task<ProductDto?> GetById(int id)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Where(p => p.ProductId == id)
            .Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Brand = p.Brand,
                BasePrice = p.BasePrice,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                IsActive = p.IsActive,
                StockQuantity = p.StockQuantity
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Product?> Add(ProductCreateRequest request)
    {
        var category = await _context.Categories.FindAsync(request.CategoryId);
        if (category == null) return null;

        var product = new Product
        {
            Name = request.Name ?? "Unnamed Product",
            Description = request.Description ?? "",
            Brand = request.Brand ?? "Unknown",
            BasePrice = request.BasePrice,
            CategoryId = request.CategoryId,
            IsActive = request.IsActive,
            StockQuantity = request.StockQuantity
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        await ClearProductsCache();

        return product;
    }

    public async Task<bool> Update(int id, ProductUpdateRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        if (!string.IsNullOrWhiteSpace(request.Name))
            product.Name = request.Name;

        if (!string.IsNullOrWhiteSpace(request.Description))
            product.Description = request.Description;

        if (!string.IsNullOrWhiteSpace(request.Brand))
            product.Brand = request.Brand;

        if (request.BasePrice.HasValue && request.BasePrice > 0)
            product.BasePrice = (decimal)request.BasePrice;

        if (request.IsActive.HasValue)
            product.IsActive = request.IsActive.Value;

        if (request.CategoryId.HasValue)
        {
            var exists = await _context.Categories.AnyAsync(c => c.CategoryId == request.CategoryId);
            if (!exists) return false;

            product.CategoryId = request.CategoryId.Value;
        }

        if (request.StockQuantity.HasValue && request.StockQuantity >= 0)
        {
            product.StockQuantity = request.StockQuantity.Value;
        }

        await _context.SaveChangesAsync();
        await ClearProductsCache();
        return true;
    }

    public async Task<bool> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        await ClearProductsCache();
        return true;
    }

    public async Task<bool> SetStock(int productId, int quantity)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null || quantity < 0) return false;

        product.StockQuantity = quantity;
        await _context.SaveChangesAsync();
        await ClearProductsCache();
        return true;
    }

    public async Task<bool> AddStock(int productId, int amount)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null || amount <= 0) return false;

        product.StockQuantity += amount;
        await _context.SaveChangesAsync();
        await ClearProductsCache();
        return true;
    }

    public async Task<bool> ReduceStock(int productId, int amount)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null || amount <= 0) return false;

        if (product.StockQuantity < amount)
            return false;

        product.StockQuantity -= amount;
        await _context.SaveChangesAsync();
        await ClearProductsCache();

        return true;
    }

    private async Task ClearProductsCache()
    {
        try
        {
            await _cache.KeyDeleteAsync(ProductsCacheKey);
        }
        catch (RedisException ex)
        {
            Console.WriteLine($"[REDIS] Product cache clear failed: {ex.Message}");
        }
    }
}
