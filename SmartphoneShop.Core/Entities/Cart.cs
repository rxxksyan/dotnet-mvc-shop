namespace SmartphoneShop.Core.Entities;

public class Cart
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual AppUser? User { get; set; }
    public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();

    public decimal TotalAmount => Items.Sum(i => i.Smartphone?.Price * i.Quantity ?? 0);
    public int TotalItems => Items.Sum(i => i.Quantity);
}