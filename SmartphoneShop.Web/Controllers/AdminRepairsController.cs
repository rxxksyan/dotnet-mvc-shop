using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Enums;
using SmartphoneShop.Infrastructure.Data;
using X.PagedList;

namespace SmartphoneShop.Web.Controllers;

[Authorize(Roles = "Admin,RepairSpecialist")]
public class AdminRepairsController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public AdminRepairsController(AppDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? status, string? search, int page = 1, int pageSize = 10)
    {
        page = Math.Max(1, page);
        var query = _context.RepairRequests
            .Include(r => r.User)
            .Include(r => r.MasterUser)
            .Include(r => r.RepairSpareParts)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<RepairStatus>(status, out var statusEnum))
            {
                query = query.Where(r => r.Status == statusEnum);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowerSearch = search.ToLower();
            query = query.Where(r => r.SmartphoneModel.ToLower().Contains(lowerSearch) ||
                                     r.IssueDescription.ToLower().Contains(lowerSearch) ||
                                     r.User.FullName.ToLower().Contains(lowerSearch));
        }

        var repairs = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToPagedListAsync(page, pageSize);

        ViewBag.Status = status;
        ViewBag.Search = search;
        ViewBag.RepairStatuses = Enum.GetValues<RepairStatus>();
        return View(repairs);
    }

    public async Task<IActionResult> Details(int id)
    {
        var repair = await _context.RepairRequests
            .Include(r => r.User)
            .Include(r => r.MasterUser)
            .Include(r => r.RepairSpareParts)
                .ThenInclude(rsp => rsp.SparePart)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (repair == null)
            return NotFound();

        return View(repair);
    }

    [HttpGet]
    public async Task<IActionResult> SearchSpareParts(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Json(Array.Empty<object>());

        var lowerQuery = query.ToLower();
        var parts = await _context.SpareParts
            .Where(s => s.Name.ToLower().Contains(lowerQuery) && s.Quantity > 0)
            .OrderBy(s => s.Name)
            .Take(10)
            .Select(s => new { id = s.Id, name = s.Name, price = s.Price, quantity = s.Quantity })
            .ToListAsync();

        return Json(parts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        int id, RepairStatus status, decimal? estimatedPrice,
        string? adminNotes, string? selectedPartsJson,
        decimal? servicePrice, string? notesForClient, bool isClientFault = false)
    {
        var repair = await _context.RepairRequests
            .Include(r => r.RepairSpareParts)
            .FirstOrDefaultAsync(r => r.Id == id);
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

        if (string.IsNullOrEmpty(repair.MasterUserId))
        {
            var currentUserId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(currentUserId))
            {
                repair.MasterUserId = currentUserId;
            }
        }

        repair.Status = status;

        if (status == RepairStatus.RepairApproval)
        {
            repair.IsClientFault = isClientFault;

            if (repair.IsWarranty && !isClientFault)
            {
                repair.EstimatedPrice = 0;
                repair.ServicePrice = 0;
            }
            else
            {
                if (!estimatedPrice.HasValue || estimatedPrice.Value <= 0)
                {
                    TempData["Error"] = "Укажите стоимость диагностики.";
                    return RedirectToAction(nameof(Index));
                }
                repair.EstimatedPrice = estimatedPrice.Value;
            }
        }
        else if (estimatedPrice.HasValue)
        {
            repair.EstimatedPrice = estimatedPrice.Value;
        }

        if (servicePrice.HasValue)
        {
            repair.ServicePrice = servicePrice.Value;
        }

        if (!string.IsNullOrEmpty(adminNotes))
        {
            repair.AdminNotes = adminNotes;
        }

        if (!string.IsNullOrEmpty(notesForClient))
        {
            repair.NotesForClient = notesForClient;
        }

        if (status == RepairStatus.RepairApproval && !string.IsNullOrEmpty(selectedPartsJson))
        {
            var existingParts = _context.RepairSpareParts
                .Where(rsp => rsp.RepairRequestId == id);
            _context.RepairSpareParts.RemoveRange(existingParts);

            var selectedParts = JsonSerializer.Deserialize<List<SelectedPartDto>>(selectedPartsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (selectedParts != null)
            {
                foreach (var part in selectedParts)
                {
                    var repairSparePart = new RepairSparePart
                    {
                        RepairRequestId = id,
                        SparePartId = part.SparePartId,
                        SparePartName = part.Name,
                        SparePartPrice = part.Price,
                        IsAvailable = part.IsAvailable,
                        EstimatedWaitDays = part.IsAvailable ? null : part.EstimatedWaitDays
                    };
                    _context.RepairSpareParts.Add(repairSparePart);
                }
            }
        }

        repair.UpdatedAt = DateTime.UtcNow;

        if (status == RepairStatus.Completed)
        {
            var partsToDecrement = await _context.RepairSpareParts
                .Where(rsp => rsp.RepairRequestId == id && rsp.SparePartId.HasValue && rsp.IsAvailable)
                .ToListAsync();
            foreach (var rsp in partsToDecrement)
            {
                var sparePart = await _context.SpareParts.FindAsync(rsp.SparePartId.Value);
                if (sparePart != null)
                {
                    sparePart.Quantity = Math.Max(0, sparePart.Quantity - 1);
                }
            }
        }

        await _context.SaveChangesAsync();

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
                false => new List<RepairStatus>(),
                _ => new List<RepairStatus> { RepairStatus.Cancelled }
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

    public class SelectedPartDto
    {
        public int? SparePartId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; } = true;
        public int? EstimatedWaitDays { get; set; }
    }
}
