namespace Webshop
{
    public class Wishlist
    {
        public int WishlistId { get; set; }
        public int CustomerId { get; set; }

        public Customer Customer { get; set; } = null!;
        public ICollection<WishlistItem> Items { get; set; } = new List<WishlistItem>();
    }
}