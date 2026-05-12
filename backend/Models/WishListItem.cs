public class WishlistItem
{
    public int WishlistItemId { get; set; }
    public int ProductVariantId { get; set; }

    public Wishlist Wishlist { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
}