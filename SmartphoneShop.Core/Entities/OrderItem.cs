namespace SmartphoneShop.Core.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int SmartphoneId { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtPurchase { get; set; }

    public virtual Order Order { get; set; } = null!;
    public virtual Smartphone Smartphone { get; set; } = null!;

    public decimal TotalPrice => PriceAtPurchase * Quantity;
}