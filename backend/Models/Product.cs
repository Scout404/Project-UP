public class Product
{
    public int Id { get; set; }

    public required string Name { get; set; }
    public decimal BasePrice { get; set; }
    public int Stock { get; set; }

    public required string Category { get; set; }

    public bool IsActive { get; set; }
}