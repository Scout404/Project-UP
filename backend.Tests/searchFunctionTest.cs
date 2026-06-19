using backend.Data;
using backend.Logic;
using Xunit;
using System.Text.Json;
using System.Reflection;

namespace backend.Tests;

public class searchFunctionTest
{
    [Fact]
    public void returnSearchedProduct()
    {

        Directory.CreateDirectory("Data");
        var testPath = "Data/test_products.json";

        var products = new List<Product>
        {
            new Product
            {
                ProductId = 1,
                Name = "White shirt",
                Description = "White cotton shirt",
                Brand = "Brand 1",
                BasePrice = 15,
                CategoryId = 1,
                IsActive = true,
                Variants = new List<ProductVariant>()
            },
            new Product
            {
                ProductId = 2,
                Name = "Black shirt",
                Description = "Black cotton shirt",
                Brand = "Brand 2",
                BasePrice = 50,
                CategoryId = 2,
                IsActive = true,
                Variants = new List<ProductVariant>()
            }
        };

        File.WriteAllText(testPath, JsonSerializer.Serialize(products));

        var search = new SearchFunction();

        // filepath veranderen
        var changePath = typeof(SearchFunction).GetField("_filePath", BindingFlags.NonPublic | BindingFlags.Instance);

        changePath.SetValue(search, testPath);

        var res = search.Search("White", null, null, null, null, null, null);

        // Assert
        Assert.Single(res);
        Assert.Equal("White shirt", res[0].Name);

        File.Delete(testPath);
    }

    [Fact]
    public void returnOnlyActiveProducts()
    {

        Directory.CreateDirectory("Data");
        var testPath = "Data/test_products.json";

        var products = new List<Product>
        {
            new Product
            {
                ProductId = 1,
                Name = "Active shirt",
                Description = "Active",
                Brand = "Brand 1",
                BasePrice = 20,
                CategoryId = 1,
                IsActive = true,
                Variants = new List<ProductVariant>()
            },
            new Product
            {
                ProductId = 2,
                Name = "Inactive shirt",
                Description = "Not active",
                Brand = "Brand 2",
                BasePrice = 30,
                CategoryId = 1,
                IsActive = false,
                Variants = new List<ProductVariant>()
            }
        };

        File.WriteAllText(testPath, JsonSerializer.Serialize(products));

        var search = new SearchFunction();


        // filepath veranderen
        var changePath = typeof(SearchFunction).GetField("_filePath", BindingFlags.NonPublic | BindingFlags.Instance);

        changePath.SetValue(search, testPath);

        var res = search.Search(null, null, null, null, null, null, null);

        Assert.Single(res);
        Assert.Equal("Active shirt", res[0].Name);

        File.Delete(testPath);
    }
}
