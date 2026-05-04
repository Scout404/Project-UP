public class WishlistItem
{
    public int WishlistItemId { get; set; }
    public int VariantId { get; set; }

    public Wishlist Wishlist { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
}