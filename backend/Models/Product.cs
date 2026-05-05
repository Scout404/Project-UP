public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int CategoryId { get; set; }
    public string Brand { get; set; } = null!;
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Category Category { get; set; } = null!;
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}