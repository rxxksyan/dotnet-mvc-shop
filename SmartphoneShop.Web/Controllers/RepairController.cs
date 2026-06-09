using Microsoft.AspNetCore.Mvc;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Enums;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Infrastructure.Data;

namespace SmartphoneShop.Web.Controllers;

public class RepairController : Controller
{
    private readonly IRepairRequestRepository _repairRepo;
    private readonly AppDbContext _context;

    public RepairController(IRepairRequestRepository repairRepo, AppDbContext context)
    {
        _repairRepo = repairRepo;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var requests = await _repairRepo.GetByUserIdAsync(userId);
            ViewBag.Repairs = requests;
        }
        return View();
    }

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Create(string smartphoneModel, string issueDescription)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return RedirectToAction("Login", "Account");

        if (string.IsNullOrEmpty(smartphoneModel) || string.IsNullOrEmpty(issueDescription))
        {
            TempData["Error"] = "Заполните все поля";
            return RedirectToAction("Index");
        }

        var request = new RepairRequest
        {
            UserId = userId,
            SmartphoneModel = smartphoneModel,
            IssueDescription = issueDescription,
            Status = RepairStatus.New
        };

        await _repairRepo.AddAsync(request);
        return RedirectToAction("Index");
    }

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Respond(int id, string message, bool? approve)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return RedirectToAction("Login", "Account");

        var request = await _context.RepairRequests.FindAsync(id);
        if (request == null || request.UserId != userId)
            return NotFound();

        if (!string.IsNullOrEmpty(message))
        {
            request.ClientMessage = message;
        }

        if (approve.HasValue)
        {
            request.ClientApproved = approve.Value;
            if (approve.Value)
            {
                request.Status = RepairStatus.InRepair;
            }
            else
            {
                request.Status = RepairStatus.Cancelled;
            }
        }

        request.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
