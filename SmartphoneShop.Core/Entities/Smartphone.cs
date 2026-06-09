namespace SmartphoneShop.Core.Entities;

public class Smartphone
{
    public int Id { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OldPrice { get; set; }
    public DateTime? ReleaseDate { get; set; }

    public decimal? ScreenSize { get; set; }
    public string? ScreenResolution { get; set; }
    public string? ScreenType { get; set; }

    public int? BatteryCapacity { get; set; }
    public int? RAM { get; set; }
    public int? Storage { get; set; }
    public string? Processor { get; set; }
    public string? MainCamera { get; set; }
    public string? FrontCamera { get; set; }
    public string? OS { get; set; }
    public bool NFC { get; set; }
    public bool WirelessCharging { get; set; }
    public string? WaterResistance { get; set; }
    public decimal? Weight { get; set; }

    public string? Colors { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageUrls { get; set; }

    public string? Description { get; set; }
    public bool IsInStock { get; set; } = true;
    public bool IsFeatured { get; set; }
    public int PopularityScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public virtual ICollection<ComparisonItem> ComparisonItems { get; set; } = new List<ComparisonItem>();
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public virtual ICollection<ExpertOpinion> ExpertOpinions1 { get; set; } = new List<ExpertOpinion>();
    public virtual ICollection<ExpertOpinion> ExpertOpinions2 { get; set; } = new List<ExpertOpinion>();
}
