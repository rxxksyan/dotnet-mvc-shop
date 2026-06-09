namespace SmartphoneShop.Web.Controllers;

public class CartItemModel
{
    public int SmartphoneId { get; set; }
    public string ModelName { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }
}
