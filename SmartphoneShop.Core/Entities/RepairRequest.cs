using SmartphoneShop.Core.Enums;

namespace SmartphoneShop.Core.Entities;

public class RepairRequest
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string SmartphoneModel { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string IssueDescription { get; set; } = string.Empty;
    public RepairStatus Status { get; set; } = RepairStatus.New;
    public decimal? EstimatedPrice { get; set; }
    public string? AdminNotes { get; set; }
    public string? NotesForClient { get; set; }
    public decimal? ServicePrice { get; set; }
    public string? ClientMessage { get; set; }
    public bool? ClientApproved { get; set; }
    public bool IsWarranty { get; set; }
    public bool IsClientFault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? MasterUserId { get; set; }
    public virtual AppUser? MasterUser { get; set; }
    public virtual AppUser User { get; set; } = null!;
    public virtual ICollection<RepairSparePart> RepairSpareParts { get; set; } = new List<RepairSparePart>();
}