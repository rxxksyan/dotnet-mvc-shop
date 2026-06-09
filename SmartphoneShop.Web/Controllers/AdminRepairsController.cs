using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Enums;
using SmartphoneShop.Infrastructure.Data;
using X.PagedList;

namespace SmartphoneShop.Web.Controllers;

[Authorize(Roles = "Admin,RepairSpecialist")]
public class AdminRepairsController : Controller
{
    private readonly AppDbContext _context;

    public AdminRepairsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? status, int page = 1, int pageSize = 10)
    {
        page = Math.Max(1, page);
        var query = _context.RepairRequests
            .Include(r => r.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<RepairStatus>(status, out var statusEnum))
            {
                query = query.Where(r => r.Status == statusEnum);
            }
        }

        var repairs = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToPagedListAsync(page, pageSize);

        ViewBag.Status = status;
        ViewBag.RepairStatuses = Enum.GetValues<RepairStatus>();
        return View(repairs);
    }

    public async Task<IActionResult> Details(int id)
    {
        var repair = await _context.RepairRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);
        
        if (repair == null)
            return NotFound();

        return View(repair);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, RepairStatus status, decimal? estimatedPrice, string? adminNotes)
    {
        var repair = await _context.RepairRequests.FindAsync(id);
        if (repair == null)
            return NotFound();

        var currentStatus = repair.Status;

        if (repair.ClientApproved == false && currentStatus == RepairStatus.Cancelled)
        {
            TempData["Error"] = "Заявка отклонена клиентом. Изменение статуса невозможно.";
            return RedirectToAction(nameof(Index));
        }

        if (currentStatus == RepairStatus.RepairApproval)
        {
            if (!repair.ClientApproved.HasValue)
            {
                if (status != RepairStatus.Cancelled)
                {
                    TempData["Error"] = "Нельзя изменить статус - ожидается одобрение клиента. Доступно только: Отменён.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else if (repair.ClientApproved == false)
            {
                TempData["Error"] = "Клиент отклонил ремонт. Заявка отменена.";
                return RedirectToAction(nameof(Index));
            }
            else if (repair.ClientApproved == true)
            {
                if (status != RepairStatus.InRepair && status != RepairStatus.Cancelled)
                {
                    TempData["Error"] = "После одобрения клиента доступны только: В ремонте, Отменён.";
                    return RedirectToAction(nameof(Index));
                }
            }
        }

        var allowedStatuses = GetAllowedNextStatuses(currentStatus, repair.ClientApproved);
        
        if (!allowedStatuses.Contains(status))
        {
            TempData["Error"] = $"Нельзя изменить статус с '{GetStatusDisplayName(currentStatus)}' на '{GetStatusDisplayName(status)}'. Доступны: {string.Join(", ", allowedStatuses.Select(GetStatusDisplayName))}";
            return RedirectToAction(nameof(Index));
        }

        var statusOrder = new List<RepairStatus>
        {
            RepairStatus.New,
            RepairStatus.DeliveryToCenter,
            RepairStatus.AcceptedAtCenter,
            RepairStatus.InQueue,
            RepairStatus.Diagnostics,
            RepairStatus.RepairApproval,
            RepairStatus.InRepair,
            RepairStatus.ReadyForPickup,
            RepairStatus.Completed
        };
        
        var currentIndex = statusOrder.IndexOf(currentStatus);
        var newIndex = statusOrder.IndexOf(status);
        
        if (status != RepairStatus.Cancelled && newIndex < currentIndex)
        {
            TempData["Error"] = "Нельзя откатывать статус назад.";
            return RedirectToAction(nameof(Index));
        }

        if (status != RepairStatus.Cancelled && newIndex > currentIndex + 1)
        {
            TempData["Error"] = "Нельзя перескакивать через статус. Переходите только к следующему по порядку.";
            return RedirectToAction(nameof(Index));
        }

        repair.Status = status;
        
        if (estimatedPrice.HasValue)
        {
            repair.EstimatedPrice = estimatedPrice.Value;
        }
        
        if (!string.IsNullOrEmpty(adminNotes))
        {
            repair.AdminNotes = adminNotes;
        }
        
        repair.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["Message"] = $"Статус заявки #{id} изменён на '{GetStatusDisplayName(status)}'";

        return RedirectToAction(nameof(Index));
    }

    private List<RepairStatus> GetAllowedNextStatuses(RepairStatus currentStatus, bool? clientApproved)
    {
        if (currentStatus == RepairStatus.Completed || currentStatus == RepairStatus.Cancelled)
        {
            return new List<RepairStatus>();
        }

        return currentStatus switch
        {
            RepairStatus.New => new List<RepairStatus>
            {
                RepairStatus.DeliveryToCenter,
                RepairStatus.Cancelled
            },
            RepairStatus.DeliveryToCenter => new List<RepairStatus>
            {
                RepairStatus.AcceptedAtCenter,
                RepairStatus.Cancelled
            },
            RepairStatus.AcceptedAtCenter => new List<RepairStatus>
            {
                RepairStatus.InQueue,
                RepairStatus.Cancelled
            },
            RepairStatus.InQueue => new List<RepairStatus>
            {
                RepairStatus.Diagnostics,
                RepairStatus.Cancelled
            },
            RepairStatus.Diagnostics => new List<RepairStatus>
            {
                RepairStatus.RepairApproval,
                RepairStatus.Cancelled
            },
            RepairStatus.RepairApproval => clientApproved switch
            {
                true => new List<RepairStatus> { RepairStatus.InRepair, RepairStatus.Cancelled },
                false => new List<RepairStatus>(), // Уже должно быть Cancelled
                _ => new List<RepairStatus> { RepairStatus.Cancelled } // Ждём клиента, можно только отменить
            },
            RepairStatus.InRepair => new List<RepairStatus>
            {
                RepairStatus.ReadyForPickup,
                RepairStatus.Cancelled
            },
            RepairStatus.ReadyForPickup => new List<RepairStatus>
            {
                RepairStatus.Completed,
                RepairStatus.Cancelled
            },
            _ => new List<RepairStatus>()
        };
    }

    private string GetStatusDisplayName(RepairStatus status)
    {
        return status switch
        {
            RepairStatus.New => "Новая",
            RepairStatus.DeliveryToCenter => "Доставка в центр",
            RepairStatus.AcceptedAtCenter => "Принят в центр",
            RepairStatus.InQueue => "В очереди",
            RepairStatus.Diagnostics => "Диагностика",
            RepairStatus.RepairApproval => "Ожидает одобрения",
            RepairStatus.InRepair => "В ремонте",
            RepairStatus.ReadyForPickup => "Готов к выдаче",
            RepairStatus.Completed => "Завершён",
            RepairStatus.Cancelled => "Отменён",
            _ => status.ToString()
        };
    }
}
