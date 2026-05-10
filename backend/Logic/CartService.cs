using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Logic;

public class CartService
{
    private readonly AppDbContext _db;

    public CartService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CartDto> GetCart(int userId)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart
            {
                UserId = userId,
                Items = new List<CartItem>()
            };

            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }

        return MapCart(cart);
    }

    public async Task<CartDto> AddToCart(int userId, AddToCartRequest req)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart
            {
                UserId = userId,
                Items = new List<CartItem>()
            };

            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.ProductId == req.VariantId);

        if (product == null)
            throw new Exception("Product not found");

        var existing = cart.Items
            .FirstOrDefault(x => x.VariantId == req.VariantId);

        if (existing != null)
        {
            existing.Quantity += req.Quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                VariantId = req.VariantId,
                Name = product.Name,
                Price = product.BasePrice,
                Quantity = req.Quantity
            });
        }

        await _db.SaveChangesAsync();

        return MapCart(cart);
    }

    public async Task<CartDto?> RemoveFromCart(int userId, int variantId)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
            return null;

        var item = cart.Items
            .FirstOrDefault(i => i.VariantId == variantId);

        if (item != null)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }

        return MapCart(cart);
    }

    private CartDto MapCart(Cart cart)
    {
        return new CartDto
        {
            UserId = cart.UserId,
            Items = cart.Items.Select(i => new CartItemDto
            {
                VariantId = i.VariantId,
                Name = i.Name ?? "Unknown",
                Price = i.Price,
                Quantity = i.Quantity
            }).ToList()
        };
    }
}