namespace SmartphoneShop.Web.Models;

public class AdminReportViewModel
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AvgOrderValue { get; set; }
    public int TotalRepairs { get; set; }
    public decimal AvgRepairCost { get; set; }
    public int TotalUsers { get; set; }
    public int TotalProducts { get; set; }

    public List<StatusItem> OrdersByStatus { get; set; } = [];
    public List<MonthlyItem> MonthlyRevenue { get; set; } = [];
    public List<ProductItem> TopSellingProducts { get; set; } = [];
    public List<BrandItem> BrandSales { get; set; } = [];
    public List<ProductItem> UnsoldProducts { get; set; } = [];
    public List<StatusItem> RepairsByStatus { get; set; } = [];
    public List<ProductItem> TopRepairModels { get; set; } = [];
    public List<MonthlyItem> NewUsersByMonth { get; set; } = [];
}

public class StatusItem
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class MonthlyItem
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int Count { get; set; }
}

public class ProductItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BrandItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}
