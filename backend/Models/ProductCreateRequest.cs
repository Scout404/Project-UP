public class ProductCreateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public decimal BasePrice { get; set; }
    public int CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}