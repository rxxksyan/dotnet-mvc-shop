namespace SmartphoneShop.Core.Entities;

public class ComparisonList
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual AppUser? User { get; set; }
    public virtual ICollection<ComparisonItem> Items { get; set; } = new List<ComparisonItem>();
}