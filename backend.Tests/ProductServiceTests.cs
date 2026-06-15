using backend.Data;
using backend.Logic;

namespace backend.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task Add_RejectsInvalidPriceAndNegativeStockWithoutRepositoryMutation()
    {
        var products = new FakeProductRepository();
        var cache = new FakeProductCache();
        var service = new ProductService(products, cache);

        var invalidPrice = await service.Add(new ProductCreateRequest
        {
            Name = "Premium Wollen Mantel",
            BasePrice = 0,
            StockQuantity = 20,
            CategoryId = 2
        });

        var invalidStock = await service.Add(new ProductCreateRequest
        {
            Name = "Premium Wollen Mantel",
            BasePrice = 199.99m,
            StockQuantity = -1,
            CategoryId = 2
        });

        Assert.Null(invalidPrice);
        Assert.Null(invalidStock);
        Assert.Equal(0, products.AddCalls);
        Assert.Equal(0, cache.ClearProductsCalls);
    }

    [Fact]
    public async Task Add_NormalizesProductDataAndClearsProductsCache()
    {
        var products = new FakeProductRepository();
        var cache = new FakeProductCache();
        var service = new ProductService(products, cache);

        var created = await service.Add(new ProductCreateRequest
        {
            Name = "  Premium Wollen Mantel  ",
            Description = "  Warm coat  ",
            Brand = "  Newlook  ",
            BasePrice = 199.99m,
            StockQuantity = 20,
            CategoryId = 2,
            IsActive = true,
            ColorName = "  Black  ",
            SizeName = "  L  "
        });

        Assert.NotNull(created);
        Assert.Equal("Premium Wollen Mantel", products.LastAddedRequest!.Name);
        Assert.Equal("Warm coat", products.LastAddedRequest.Description);
        Assert.Equal("Newlook", products.LastAddedRequest.Brand);
        Assert.Equal("Black", products.LastAddedRequest.ColorName);
        Assert.Equal("L", products.LastAddedRequest.SizeName);
        Assert.Equal(1, cache.ClearProductsCalls);
    }

    [Fact]
    public async Task ReduceStock_DoesNotMutateWhenRequestedAmountExceedsCurrentStock()
    {
        var products = new FakeProductRepository
        {
            StockByProductId = { [5] = 3 }
        };
        var cache = new FakeProductCache();
        var service = new ProductService(products, cache);

        var result = await service.ReduceStock(5, 4);

        Assert.False(result);
        Assert.Equal(3, products.StockByProductId[5]);
        Assert.Equal(0, products.SetStockCalls);
        Assert.Equal(0, cache.ClearProductsCalls);
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public Dictionary<int, int> StockByProductId { get; init; } = new();
        public int AddCalls { get; private set; }
        public int SetStockCalls { get; private set; }
        public ProductCreateRequest? LastAddedRequest { get; private set; }

        public Task<List<ProductDto>> GetAll()
        {
            return Task.FromResult(new List<ProductDto>());
        }

        public Task<ProductDto?> GetById(int id)
        {
            return Task.FromResult<ProductDto?>(new ProductDto
            {
                ProductId = id,
                Name = "Existing",
                Brand = "Brand",
                BasePrice = 10m,
                CategoryId = 1,
                CategoryName = "clothes",
                IsActive = true,
                StockQuantity = 3
            });
        }

        public Task<ProductDto?> Add(ProductCreateRequest request)
        {
            AddCalls++;
            LastAddedRequest = request;

            return Task.FromResult<ProductDto?>(new ProductDto
            {
                ProductId = 100,
                Name = request.Name ?? "",
                Description = request.Description,
                Brand = request.Brand ?? "",
                BasePrice = request.BasePrice,
                StockQuantity = request.StockQuantity,
                CategoryId = request.CategoryId,
                CategoryName = "clothes",
                IsActive = request.IsActive,
                ColorName = request.ColorName,
                SizeName = request.SizeName
            });
        }

        public Task<bool> Update(int id, ProductUpdateRequest request)
        {
            return Task.FromResult(true);
        }

        public Task<bool> Delete(int id)
        {
            return Task.FromResult(true);
        }

        public Task<int?> GetStock(int productId)
        {
            return Task.FromResult(
                StockByProductId.TryGetValue(productId, out var stock)
                    ? stock
                    : (int?)null);
        }

        public Task<bool> SetStock(int productId, int quantity)
        {
            SetStockCalls++;
            StockByProductId[productId] = quantity;
            return Task.FromResult(true);
        }

        public Task<bool> SetImage(int productId, string imageUrl)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class FakeProductCache : IProductCache
    {
        public int ClearProductsCalls { get; private set; }

        public Task<List<ProductDto>?> GetProducts()
        {
            return Task.FromResult<List<ProductDto>?>(null);
        }

        public Task SetProducts(List<ProductDto> products)
        {
            return Task.CompletedTask;
        }

        public Task ClearProducts()
        {
            ClearProductsCalls++;
            return Task.CompletedTask;
        }
    }
}
