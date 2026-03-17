public class Customer
{
    public int CustomerId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public string? Phone { get; set; }
    public bool IsMember { get; set; } = false;
    public DateTime? CreatedAt { get; set; }
    
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<Orders> Orders { get; set; } = new List<Orders>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public Wishlist? Wishlist { get; set; }
}