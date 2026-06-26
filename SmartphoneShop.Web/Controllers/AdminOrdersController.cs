using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;
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
        var allowed = GetAllowedNextStatuses(current, order.DeliveryType);

        if (!allowed.Contains(status))
        {
            return RedirectToAction(nameof(Index));
        }

        var flow = new List<OrderStatus> { OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Shipped, OrderStatus.Delivered };
        var curIdx = flow.IndexOf(current);
        var newIdx = flow.IndexOf(status);
        if (status != OrderStatus.Cancelled && (newIdx < curIdx || newIdx > curIdx + 1))
        {
            if (!(order.DeliveryType == DeliveryType.Pickup && current == OrderStatus.Confirmed && status == OrderStatus.Delivered))
            {
                return RedirectToAction(nameof(Index));
            }
        }

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private static List<OrderStatus> GetAllowedNextStatuses(OrderStatus current, DeliveryType dt)
    {
        if (dt == DeliveryType.Pickup)
        {
            return current switch
            {
                OrderStatus.Pending => new() { OrderStatus.Confirmed, OrderStatus.Cancelled },
                OrderStatus.Confirmed => new() { OrderStatus.Delivered, OrderStatus.Cancelled },
                _ => new()
            };
        }
        return current switch
        {
            OrderStatus.Pending => new() { OrderStatus.Confirmed, OrderStatus.Cancelled },
            OrderStatus.Confirmed => new() { OrderStatus.Shipped, OrderStatus.Cancelled },
            OrderStatus.Shipped => new() { OrderStatus.Delivered },
            _ => new()
        };
    }

    public static string GetStatusDisplayName(OrderStatus s, DeliveryType dt) => (s, dt) switch
    {
        (OrderStatus.Pending, _) => "Новый",
        (OrderStatus.Confirmed, DeliveryType.Pickup) => "Готов к выдаче",
        (OrderStatus.Confirmed, DeliveryType.Delivery) => "Упаковка",
        (OrderStatus.Shipped, DeliveryType.Delivery) => "Доставка",
        (OrderStatus.Delivered, DeliveryType.Pickup) => "Получен",
        (OrderStatus.Delivered, DeliveryType.Delivery) => "Завершён",
        (OrderStatus.Cancelled, _) => "Отменён",
        _ => s.ToString()
    };
}
