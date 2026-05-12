using System.Text.Json.Serialization;

public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int CategoryId { get; set; }
    public string Brand { get; set; } = null!;
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public int StockQuantity { get; set; }
    
    // Navigation
    public Category Category { get; set; } = null!;
    [JsonIgnore]
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    [JsonIgnore]
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}