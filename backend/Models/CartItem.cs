using backend.Models;

public class CartItem
{
    public int Id { get; set; }

    public int VariantId { get; set; }

    public string Name { get; set; } = "";
    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;
}