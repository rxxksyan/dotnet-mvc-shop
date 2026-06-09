using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Infrastructure.Data;
using X.PagedList;

namespace SmartphoneShop.Web.Controllers;

[Authorize(Roles = "Admin,ProductAdmin")]
public class AdminPurchaseOrdersController : Controller
{
    private readonly AppDbContext _context;

    public AdminPurchaseOrdersController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? status, int page = 1, int pageSize = 10)
    {
        var query = _context.PurchaseOrders
            .Include(po => po.User)
            .Include(po => po.Smartphone)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(po => po.Status == status);
        }

        var orders = await query
            .OrderByDescending(po => po.CreatedAt)
            .ToPagedListAsync(page, pageSize);

        ViewBag.Status = status;
        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsProcessed(int id)
    {
        var order = await _context.PurchaseOrders.FindAsync(id);
        if (order == null)
            return NotFound();

        order.Status = "Processed";
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}