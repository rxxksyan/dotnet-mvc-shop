using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Enums;
using SmartphoneShop.Infrastructure.Data;
using SmartphoneShop.Web.Models;
using SmartphoneShop.Web.Services;

namespace SmartphoneShop.Web.Controllers;

[Authorize(Roles = "Admin,ProductAdmin")]
public class AdminReportsController : Controller
{
    private readonly AppDbContext _context;
    private readonly ReportGenerator _reportGenerator;

    public AdminReportsController(AppDbContext context, ReportGenerator reportGenerator)
    {
        _context = context;
        _reportGenerator = reportGenerator;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Download()
    {
        var data = await LoadReportDataAsync();
        var bytes = _reportGenerator.GenerateReport(data);
        var fileName = $"rxxMRKT_Report_{DateTime.UtcNow:yyyy-MM-dd}.docx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
    }

    private async Task<AdminReportViewModel> LoadReportDataAsync()
    {
        var model = new AdminReportViewModel();

        model.TotalOrders = await _context.Orders.CountAsync();
        model.TotalRevenue = await _context.OrderItems.SumAsync(oi => oi.PriceAtPurchase * oi.Quantity);
        model.AvgOrderValue = model.TotalOrders > 0 ? model.TotalRevenue / model.TotalOrders : 0;
        model.TotalProducts = await _context.Smartphones.CountAsync();
        model.TotalUsers = await _context.Users.CountAsync();

        model.TotalRepairs = await _context.RepairRequests.CountAsync();
        var repairsWithPrice = await _context.RepairRequests.Where(r => r.EstimatedPrice.HasValue).ToListAsync();
        model.AvgRepairCost = repairsWithPrice.Count > 0
            ? repairsWithPrice.Average(r => r.EstimatedPrice!.Value)
            : 0;

        model.OrdersByStatus = await _context.Orders
            .GroupBy(o => o.Status)
            .Select(g => new StatusItem { Label = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var months = Enumerable.Range(0, 12).Select(i => DateTime.UtcNow.AddMonths(-i)).Reverse().ToList();
        var revenueData = await _context.Orders
            .Where(o => o.CreatedAt >= months.First())
            .SelectMany(o => o.Items)
            .GroupBy(oi => new { oi.Order!.CreatedAt.Year, oi.Order.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(oi => oi.PriceAtPurchase * oi.Quantity) })
            .ToListAsync();

        var orderCountByMonth = await _context.Orders
            .Where(o => o.CreatedAt >= months.First())
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync();

        model.MonthlyRevenue = [];
        foreach (var m in months)
        {
            var rev = revenueData.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month);
            var cnt = orderCountByMonth.FirstOrDefault(c => c.Year == m.Year && c.Month == m.Month);
            model.MonthlyRevenue.Add(new MonthlyItem
            {
                Label = $"{m.Year}-{m.Month:D2}",
                Value = rev?.Revenue ?? 0,
                Count = cnt?.Count ?? 0
            });
        }

        model.TopSellingProducts = await _context.OrderItems
            .GroupBy(oi => new { oi.SmartphoneId, Name = oi.Smartphone.Brand + " " + oi.Smartphone.ModelName })
            .Select(g => new ProductItem
            {
                Name = g.Key.Name,
                Quantity = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.PriceAtPurchase * oi.Quantity)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(10)
            .ToListAsync();

        model.BrandSales = await _context.OrderItems
            .GroupBy(oi => oi.Smartphone.Brand)
            .Select(g => new BrandItem
            {
                Name = g.Key,
                Quantity = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.PriceAtPurchase * oi.Quantity)
            })
            .OrderByDescending(b => b.Quantity)
            .ToListAsync();

        model.UnsoldProducts = await _context.Smartphones
            .Where(s => !s.OrderItems.Any())
            .Select(s => new ProductItem
            {
                Name = s.Brand + " " + s.ModelName,
                Price = s.Price,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        model.RepairsByStatus = await _context.RepairRequests
            .GroupBy(r => r.Status)
            .Select(g => new StatusItem { Label = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        model.TopRepairModels = await _context.RepairRequests
            .GroupBy(r => r.SmartphoneModel)
            .Select(g => new ProductItem { Name = g.Key, Quantity = g.Count() })
            .OrderByDescending(p => p.Quantity)
            .Take(5)
            .ToListAsync();

        var userData = await _context.Users
            .Where(u => u.CreatedAt >= months.First())
            .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync();

        model.NewUsersByMonth = [];
        foreach (var m in months)
        {
            var ud = userData.FirstOrDefault(u => u.Year == m.Year && u.Month == m.Month);
            model.NewUsersByMonth.Add(new MonthlyItem
            {
                Label = $"{m.Year}-{m.Month:D2}",
                Count = ud?.Count ?? 0
            });
        }

        return model;
    }
}
