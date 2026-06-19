using backend.Data;
using backend.Logic;
using backend.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace backend.Tests;

public class CheckoutServiceTests
{
    [Fact]
    public async Task Checkout_ReducesStockCreatesOrderAndWritesReceiptFile()
    {
        var tempRoot = CreateTempRoot();
        var repository = new FakeCheckoutRepository
        {
            StockByProductId = { [10] = 12 }
        };
        var service = new CheckoutService(repository, new FakeWebHostEnvironment(tempRoot));

        var result = await service.Checkout(CreateCheckoutRequest());

        Assert.True(result.Success);
        Assert.Equal(10, repository.StockByProductId[10]);
        Assert.Equal(1, repository.CreateOrderCalls);
        Assert.Equal(160m, repository.CreatedOrderTotal);
        Assert.True(File.Exists(service.ReceiptFilePath));

        var receipt = await File.ReadAllTextAsync(service.ReceiptFilePath);
        Assert.Contains("===== ORDER RECEIPT =====", receipt);
        Assert.Contains("Mark Tester", receipt);
        Assert.Contains("Chino Pants x2", receipt);
        Assert.Contains("TOTAL:", receipt);

        Directory.Delete(tempRoot, recursive: true);
    }

    [Fact]
    public async Task Checkout_WithInsufficientStockDoesNotCreateOrderOrReceipt()
    {
        var tempRoot = CreateTempRoot();
        var repository = new FakeCheckoutRepository
        {
            StockByProductId = { [10] = 1 }
        };
        var service = new CheckoutService(repository, new FakeWebHostEnvironment(tempRoot));

        var result = await service.Checkout(CreateCheckoutRequest());

        Assert.False(result.Success);
        Assert.Equal("Not enough stock for: Chino Pants", result.ErrorMessage);
        Assert.Equal(0, repository.CreateOrderCalls);
        Assert.False(File.Exists(service.ReceiptFilePath));

        Directory.Delete(tempRoot, recursive: true);
    }

    private static CheckoutRequest CreateCheckoutRequest()
    {
        return new CheckoutRequest
        {
            FirstName = "Mark",
            LastName = "Tester",
            Email = "mark.tester@example.com",
            Street = "Coolsingel 1",
            City = "Rotterdam",
            PostalCode = "3012 AA",
            Country = "Netherlands",
            PaymentMethod = "Mock Payment",
            Items =
            [
                new CartItemDto
                {
                    VariantId = 10,
                    Name = "Chino Pants",
                    Price = 80m,
                    Quantity = 2
                }
            ]
        };
    }

    private static string CreateTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"project-up-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FakeCheckoutRepository : ICheckoutRepository
    {
        public Dictionary<int, int> StockByProductId { get; init; } = new();
        public int CreateOrderCalls { get; private set; }
        public decimal CreatedOrderTotal { get; private set; }

        public Task<int?> GetProductStock(int productId)
        {
            return Task.FromResult(
                StockByProductId.TryGetValue(productId, out var stock)
                    ? stock
                    : (int?)null);
        }

        public Task<bool> ReduceStock(int productId, int quantity)
        {
            if (!StockByProductId.TryGetValue(productId, out var stock) || stock < quantity)
                return Task.FromResult(false);

            StockByProductId[productId] = stock - quantity;
            return Task.FromResult(true);
        }

        public Task<int?> CreateOrder(CheckoutRequest request, decimal totalPrice)
        {
            CreateOrderCalls++;
            CreatedOrderTotal = totalPrice;
            return Task.FromResult<int?>(123);
        }

        public Task<List<OrderSummaryDto>> GetOrders()
        {
            return Task.FromResult(new List<OrderSummaryDto>());
        }
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public FakeWebHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
        }

        public string ApplicationName { get; set; } = "backend.Tests";
        public IFileProvider ContentRootFileProvider { get; set; }
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Testing";
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
