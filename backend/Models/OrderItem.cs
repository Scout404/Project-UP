public class OrderItem
{
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public int VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public Orders Order { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
}