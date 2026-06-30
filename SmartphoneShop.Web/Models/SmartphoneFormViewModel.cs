using Microsoft.AspNetCore.Http;

namespace SmartphoneShop.Web.Models;

public class SmartphoneFormViewModel
{
    public int Id { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string? Price { get; set; }
    public string? OldPrice { get; set; }
    public string? RAM { get; set; }
    public string? Storage { get; set; }
    public string? ScreenSize { get; set; }
    public string? ScreenResolution { get; set; }
    public string? ScreenType { get; set; }
    public string? BatteryCapacity { get; set; }
    public string? Processor { get; set; }
    public string? MainCamera { get; set; }
    public string? FrontCamera { get; set; }
    public string? OS { get; set; }
    public bool NFC { get; set; }
    public bool WirelessCharging { get; set; }
    public string? WaterResistance { get; set; }
    public string? Weight { get; set; }
    public string? Colors { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageUrls { get; set; }
    public IFormFile[]? ImageFiles { get; set; }
    public string? PhotosToDelete { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; } = 0;
    public bool IsFeatured { get; set; }
    public DateTime? CreatedAt { get; set; }
}