namespace SmartphoneShop.Core.Entities;

public class ComparisonItem
{
    public int Id { get; set; }
    public int ComparisonListId { get; set; }
    public int SmartphoneId { get; set; }

    public virtual ComparisonList ComparisonList { get; set; } = null!;
    public virtual Smartphone Smartphone { get; set; } = null!;
}