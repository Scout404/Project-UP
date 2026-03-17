namespace Webshop
{
    public class Category
    {
        public int CatId { get; set; }
        public string Name { get; set; } = null!;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}