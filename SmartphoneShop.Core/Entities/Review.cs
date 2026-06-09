namespace SmartphoneShop.Core.Entities;

public class Review
{
    public int Id { get; set; }
    public int SmartphoneId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual Smartphone Smartphone { get; set; } = null!;
    public virtual AppUser User { get; set; } = null!;
}