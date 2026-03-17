namespace Webshop
{
    public class Color
    {
        public int ColorId { get; set; }
        public string Name { get; set; } = null!;

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}