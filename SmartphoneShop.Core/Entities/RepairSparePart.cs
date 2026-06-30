namespace SmartphoneShop.Core.Entities;

public class RepairSparePart
{
    public int Id { get; set; }
    public int RepairRequestId { get; set; }
    public int? SparePartId { get; set; }
    public string SparePartName { get; set; } = string.Empty;
    public decimal SparePartPrice { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int? EstimatedWaitDays { get; set; }

    public virtual RepairRequest RepairRequest { get; set; } = null!;
    public virtual SparePart? SparePart { get; set; }
}
