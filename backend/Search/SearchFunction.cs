using System.Text.Json;

public class SearchFunction
{
    private readonly string _filePath = "Data/products.json";

    public List<Product> Search(string? searchedProduct,int? categoryId,string? brand,decimal? minPrice,decimal? maxPrice,int? colorId,int? sizeId)
    {
        var json = File.ReadAllText(_filePath);
        var products = JsonSerializer.Deserialize<List<Product>>(json)?? new List<Product>(); 

        var activeProducts= products.Where(x=> x.IsActive);

        if(!string.IsNullOrEmpty(searchedProduct))
        {
            activeProducts = activeProducts.Where(x=> x.Name.Contains(searchedProduct, StringComparison.OrdinalIgnoreCase)||x.Description.Contains(searchedProduct, StringComparison.OrdinalIgnoreCase) || x.Brand.Contains(searchedProduct, StringComparison.OrdinalIgnoreCase));
        }


        if(categoryId is not null)
        {
            activeProducts = activeProducts.Where(x=> x.CategoryId == categoryId.Value);
        }
        if (minPrice is not null)
        {
            activeProducts = activeProducts.Where(p => p.BasePrice >= minPrice.Value);
        }

        if (maxPrice is not null)
        {
            activeProducts = activeProducts.Where(p => p.BasePrice <= maxPrice.Value);
        }

        if(!string.IsNullOrEmpty(brand))
        {
            activeProducts = activeProducts.Where(p => p.Brand == brand);
        }

        if(colorId is not null)
        {
            activeProducts = activeProducts.Where(x =>x .Variants.Any(x => x.ColorId == colorId.Value));
        }
        if(sizeId is not null)
        {
            activeProducts = activeProducts.Where(x =>x .Variants.Any(x => x.SizeId == sizeId.Value));
        }

        return activeProducts.ToList();


    }




}