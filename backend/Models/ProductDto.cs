public class ProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; } 
    public string Brand { get; set; } = null!;
    public decimal BasePrice { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public bool IsActive { get; set; }
}