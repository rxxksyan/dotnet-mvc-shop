namespace SmartphoneShop.Core.Entities;

public class Favorite
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int SmartphoneId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual AppUser User { get; set; } = null!;
    public virtual Smartphone Smartphone { get; set; } = null!;
}