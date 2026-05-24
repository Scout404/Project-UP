using System.Text.Json;
using backend;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class SearchFunction
{
    // private readonly string _filePath = "Data/products.json";
    private readonly AppDbContext _context;

    public SearchFunction(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductDto>> Search(string? searchedProduct,int? categoryId,string? brand,decimal? minPrice,decimal? maxPrice,int? colorId,int? sizeId)
    {
        var query = _context.Products.Include(x => x.Category).Include(x => x.Variants).AsQueryable();


        if (!string.IsNullOrWhiteSpace(searchedProduct))
        {
            query = query.Where(x => x.Name.Contains(searchedProduct) || x.Description.Contains(searchedProduct) || x.Brand.Contains(searchedProduct));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(x => x.BasePrice >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(x => x.BasePrice <= maxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(brand))
        {
            query = query.Where(x => x.Brand == brand);
        }

        if (colorId.HasValue)
        {
            query = query.Where(x => x.Variants.Any(v => v.ColorId == colorId.Value));
        }

        if (sizeId.HasValue)
        {
            query = query.Where(x => x.Variants.Any(v => v.SizeId == sizeId.Value));
        }

        return await query.Select(p => new ProductDto
        {
            ProductId = p.ProductId,
            Name = p.Name,
            Description = p.Description,
            Brand = p.Brand,
            BasePrice = p.BasePrice,
            CategoryId = p.CategoryId,
            CategoryName = p.Category.Name,
            IsActive = p.IsActive
        }).ToListAsync();

        
    }



}