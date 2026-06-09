using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using X.PagedList;

namespace SmartphoneShop.Web.Controllers;

[Authorize]
public class PurchaseOrderController : Controller
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly ISmartphoneRepository _smartphoneRepository;
    private readonly UserManager<AppUser> _userManager;

    public PurchaseOrderController(
        IPurchaseOrderRepository purchaseOrderRepository,
        ISmartphoneRepository smartphoneRepository,
        UserManager<AppUser> userManager)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _smartphoneRepository = smartphoneRepository;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var purchaseOrders = await _purchaseOrderRepository.GetByUserIdPagedAsync(user.Id, page, pageSize);
        return View(purchaseOrders);
    }

    public async Task<IActionResult> Details(int id)
    {
        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(id);
        if (purchaseOrder == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null || purchaseOrder.UserId != user.Id)
        {
            return Forbid();
        }

        return View(purchaseOrder);
    }

    public async Task<IActionResult> Create(int smartphoneId)
    {
        var smartphone = await _smartphoneRepository.GetByIdAsync(smartphoneId);
        if (smartphone == null) return NotFound();
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();
        if (await _purchaseOrderRepository.UserHasPendingOrderAsync(user.Id, smartphoneId))
        {
            TempData["Error"] = "У вас уже есть активный запрос на покупку этого смартфона.";
            return RedirectToAction("Details", "Product", new { id = smartphoneId });
        }
        var purchaseOrder = new PurchaseOrder
        {
            SmartphoneId = smartphoneId,
            UserId = user.Id,
            SmartphoneName = smartphone.ModelName,
            Status = "Pending",
            CreatedAt = DateTime.Now
        };
        return View(purchaseOrder);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseOrder purchaseOrder)
    {
        if (!ModelState.IsValid)
        {
            var smartphone = await _smartphoneRepository.GetByIdAsync(purchaseOrder.SmartphoneId);
            if (smartphone != null) purchaseOrder.SmartphoneName = smartphone.ModelName;
            return View(purchaseOrder);
        }
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();
        purchaseOrder.UserId = user.Id;
        purchaseOrder.CreatedAt = DateTime.Now;
        purchaseOrder.Status = "Pending";
        try
        {
            await _purchaseOrderRepository.AddAsync(purchaseOrder);
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            TempData["Error"] = "Ошибка при создании запроса на покупку.";
            var smartphone = await _smartphoneRepository.GetByIdAsync(purchaseOrder.SmartphoneId);
            if (smartphone != null) purchaseOrder.SmartphoneName = smartphone.ModelName;
            return View(purchaseOrder);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(id);
        if (purchaseOrder == null) return NotFound();
        var user = await _userManager.GetUserAsync(User);
        if (user == null || purchaseOrder.UserId != user.Id) return Forbid();
        if (purchaseOrder.Status != "Pending")
        {
            TempData["Error"] = "Отмена возможна только для ожидающих запросов.";
            return RedirectToAction(nameof(Index));
        }
        try
        {
            await _purchaseOrderRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            TempData["Error"] = "Ошибка при отмене запроса на покупку.";
            return RedirectToAction(nameof(Index));
        }
    }

    public string GetStatusClass(string status) => status switch
    {
        "Pending" => "warning",
        "Approved" => "success",
        "Rejected" => "danger",
        "Processing" => "info",
        "Completed" => "success",
        _ => "secondary"
    };

    public string GetStatusText(string status) => status switch
    {
        "Pending" => "В ожидании",
        "Approved" => "Одобрен",
        "Rejected" => "Отклонен",
        "Processing" => "В обработке",
        "Completed" => "Завершен",
        _ => status
    };
}
