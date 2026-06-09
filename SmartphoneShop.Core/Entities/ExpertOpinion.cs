namespace SmartphoneShop.Core.Entities;

public class ExpertOpinion
{
    public int Id { get; set; }
    public int SmartphoneId1 { get; set; }
    public int SmartphoneId2 { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public string ExpertId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual AppUser? Expert { get; set; }
    public virtual Smartphone? Smartphone1 { get; set; }
    public virtual Smartphone? Smartphone2 { get; set; }
}