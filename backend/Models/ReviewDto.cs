public class ReviewDto
{
    public int ReviewId { get; set; }
    public int ProductId { get; set; }
    public int? UserId { get; set; }
    public string ReviewerName { get; set; } = null!;
    public string ReviewText { get; set; } = null!;
    public int Rating { get; set; }
    public DateTime? CreatedAt { get; set; }
}
