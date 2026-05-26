using backend.Data;
using backend.Models;

namespace backend.Logic;

public class CartService
{
    private readonly CartRepository _cartRepository;

    public CartService(CartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<CartDto> GetCart(int userId)
    {
        var cart = await _cartRepository.GetCart(userId);

        if (cart != null)
            return cart;

        await _cartRepository.CreateCart(userId);

        return new CartDto
        {
            UserId = userId,
            Items = new List<CartItemDto>()
        };
        
    }

    public async Task<CartDto> AddToCart(int userId, AddToCartRequest req)
    {
        int? cartId = await _cartRepository.GetCartId(userId);

        if (cartId == null)
        {
            cartId = await _cartRepository.CreateCart(userId);
        }

        var product = await _cartRepository.GetProduct(req.VariantId);

        if (product == null)
            throw new Exception("Product not found");

        var existingItem = await _cartRepository.GetCartItem(
            cartId.Value,
            req.VariantId);

        if (existingItem != null)
        {
            await _cartRepository.UpdateCartItemQuantity(
                cartId.Value,
                req.VariantId,
                existingItem.Quantity + req.Quantity);
        }
        else
        {
            await _cartRepository.AddCartItem(
                cartId.Value,
                req.VariantId,
                product.Name ?? "Unknown",
                product.BasePrice,
                req.Quantity);
        }

        return await GetCart(userId);
    }

    public async Task<CartDto?> RemoveFromCart(int userId, int variantId)
    {
        int? cartId = await _cartRepository.GetCartId(userId);

        if (cartId == null)
            return null;

        await _cartRepository.RemoveCartItem(
            cartId.Value,
            variantId);

        return await GetCart(userId);
    }
}