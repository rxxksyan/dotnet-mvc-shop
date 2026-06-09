namespace SmartphoneShop.Core.Entities;

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public int SmartphoneId { get; set; }
    public int Quantity { get; set; } = 1;

    public virtual Cart Cart { get; set; } = null!;
    public virtual Smartphone Smartphone { get; set; } = null!;

    public decimal TotalPrice => Smartphone?.Price * Quantity ?? 0;
}