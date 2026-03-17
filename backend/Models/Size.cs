public class Size
{
    public int SizeId { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}