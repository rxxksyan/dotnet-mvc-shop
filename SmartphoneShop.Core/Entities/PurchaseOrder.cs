namespace SmartphoneShop.Core.Entities;

public class PurchaseOrder
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int SmartphoneId { get; set; }
    public string SmartphoneName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Quantity { get; set; } = 1;
    public string? SpecialRequests { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? AdminComment { get; set; }

    public virtual AppUser? User { get; set; }
    public virtual Smartphone? Smartphone { get; set; }
}