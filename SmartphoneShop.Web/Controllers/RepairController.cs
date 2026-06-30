using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            var requests = await _context.RepairRequests
                .Include(r => r.RepairSpareParts)
                .Include(r => r.MasterUser)
                .Include(r => r.User)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            ViewBag.Repairs = requests;

            var warrantyDate = DateTime.UtcNow.AddMonths(-12);
            var warrantyRepairs = await _context.RepairRequests
                .Where(r => r.UserId == userId &&
                            r.Status == RepairStatus.Completed &&
                            r.UpdatedAt >= warrantyDate)
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();
            ViewBag.WarrantyRepairs = warrantyRepairs;
        }
        return View();
    }

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Create(string smartphoneModel, string serialNumber, string issueDescription, bool isWarranty = false)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return RedirectToAction("Login", "Account");

        if (string.IsNullOrEmpty(smartphoneModel) || string.IsNullOrEmpty(issueDescription) || string.IsNullOrEmpty(serialNumber))
        {
            TempData["Error"] = "Заполните все поля";
            return RedirectToAction("Index");
        }

        var request = new RepairRequest
        {
            UserId = userId,
            SmartphoneModel = smartphoneModel,
            SerialNumber = serialNumber,
            IssueDescription = issueDescription,
            Status = RepairStatus.New,
            IsWarranty = isWarranty
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
