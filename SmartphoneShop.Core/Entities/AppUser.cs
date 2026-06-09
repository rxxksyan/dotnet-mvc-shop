using Microsoft.AspNetCore.Identity;

namespace SmartphoneShop.Core.Entities;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<RepairRequest> RepairRequests { get; set; } = new List<RepairRequest>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual Cart? Cart { get; set; }
    public virtual ComparisonList? ComparisonList { get; set; }
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public virtual ICollection<ExpertOpinion> ExpertOpinions { get; set; } = new List<ExpertOpinion>();
}
