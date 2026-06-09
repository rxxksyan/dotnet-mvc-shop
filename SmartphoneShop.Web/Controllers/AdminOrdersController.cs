using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Enums;
using SmartphoneShop.Infrastructure.Data;
using X.PagedList;

namespace SmartphoneShop.Web.Controllers;

[Authorize(Roles = "Admin,ProductAdmin")]
public class AdminOrdersController : Controller
{
    private readonly AppDbContext _context;

    public AdminOrdersController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? status, int page = 1, int pageSize = 10)
    {
        page = Math.Max(1, page);
        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items).ThenInclude(i => i.Smartphone)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var statusEnum))
        {
            query = query.Where(o => o.Status == statusEnum);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToPagedListAsync(page, pageSize);

        ViewBag.Status = status;
        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        var current = order.Status;
        var allowed = GetAllowedNextStatuses(current);

        if (!allowed.Contains(status))
        {
            TempData["Error"] = $"Нельзя изменить статус с '{GetName(current)}' на '{GetName(status)}'.";
            return RedirectToAction(nameof(Index));
        }

        var flow = new List<OrderStatus> { OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Shipped, OrderStatus.Delivered };
        var curIdx = flow.IndexOf(current);
        var newIdx = flow.IndexOf(status);
        if (status != OrderStatus.Cancelled && (newIdx < curIdx || newIdx > curIdx + 1))
        {
            TempData["Error"] = "Нельзя перескакивать или откатывать статус.";
            return RedirectToAction(nameof(Index));
        }

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Message"] = $"Заказ #{id} — статус изменён на '{GetName(status)}'";
        return RedirectToAction(nameof(Index));
    }

    private static List<OrderStatus> GetAllowedNextStatuses(OrderStatus current)
    {
        return current switch
        {
            OrderStatus.Pending => new() { OrderStatus.Confirmed, OrderStatus.Cancelled },
            OrderStatus.Confirmed => new() { OrderStatus.Shipped, OrderStatus.Cancelled },
            OrderStatus.Shipped => new() { OrderStatus.Delivered },
            _ => new()
        };
    }

    private static string GetName(OrderStatus s) => s switch
    {
        OrderStatus.Pending => "Новый",
        OrderStatus.Confirmed => "Упаковка",
        OrderStatus.Shipped => "Доставка",
        OrderStatus.Delivered => "Завершён",
        OrderStatus.Cancelled => "Отменён",
        _ => s.ToString()
    };
}
