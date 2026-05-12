using System.Text.Json.Serialization;

public class Category
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = null!;

    [JsonIgnore]
    public ICollection<Product> Products { get; set; } = new List<Product>();
}