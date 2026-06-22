using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace SmartphoneShop.Web.Controllers;

public class ComparisonController : Controller
{
    private readonly IComparisonRepository _comparisonRepo;
    private readonly ISmartphoneRepository _smartphoneRepo;
    private readonly ILogger<ComparisonController> _logger;
    private readonly AppDbContext _context;
    private readonly UserManager<Core.Entities.AppUser> _userManager;

    public ComparisonController(IComparisonRepository comparisonRepo, ISmartphoneRepository smartphoneRepo, ILogger<ComparisonController> logger, AppDbContext context, UserManager<Core.Entities.AppUser> userManager)
    {
        _comparisonRepo = comparisonRepo;
        _smartphoneRepo = smartphoneRepo;
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var list = await GetComparisonListAsync();
        
        var expertOpinions = new List<Core.Entities.ExpertOpinion>();
        var items = list.Items.ToList();
        
        if (items != null && items.Count >= 2)
        {
            var id1 = Math.Min(items[0].SmartphoneId, items[1].SmartphoneId);
            var id2 = Math.Max(items[0].SmartphoneId, items[1].SmartphoneId);
            
            expertOpinions = await _context.ExpertOpinions
                .Include(e => e.Expert)
                .Where(e => e.SmartphoneId1 == id1 && e.SmartphoneId2 == id2)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }
        
        ViewBag.ExpertOpinions = expertOpinions;
        ViewBag.IsExpert = false;
        ViewBag.CanAddOpinion = false; // Для "народных экспертов"
        ViewBag.ComparisonCount = list.Items?.Count() ?? 0;

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = GetUserId();
            if (userId != null)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    ViewBag.IsExpert = roles.Contains("Expert") || roles.Contains("Admin");
                    ViewBag.CanAddOpinion = roles.Contains("Expert") || roles.Contains("Admin") || roles.Contains("Народный эксперт");
                }
            }
        }
        
        return View(list);
    }

    private string? GetUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }

    [HttpGet]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Count()
    {
        var list = await GetComparisonListAsync();
        int count = list?.Items?.Count() ?? 0;
        return Json(new { count });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Route("/comparison/toggle")]
    public async Task<IActionResult> RemoveBySmartphoneId(int smartphoneId)
    {
        try
        {
            var list = await GetComparisonListAsync();
            if (list == null) return NotFound();

            _logger.LogInformation("Removing smartphone {SmartphoneId} from comparison list {ListId}", smartphoneId, list.Id);
            await _comparisonRepo.RemoveItemBySmartphoneIdAsync(list.Id, smartphoneId);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from comparison");
            return BadRequest(new { success = false, message = "Ошибка при удалении из сравнения" });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Add(int smartphoneId)
    {
        try
        {
            var smartphone = await _smartphoneRepo.GetByIdAsync(smartphoneId);
            if (smartphone == null)
            {
                _logger.LogWarning("Attempt to add non-existent smartphone {SmartphoneId} to comparison | Session: {SessionId} | User: {UserId}",
                    smartphoneId, HttpContext.Session.Id, User.Identity?.Name ?? "Anonymous");
                return NotFound();
            }

            var list = await GetComparisonListAsync();

            if (list.Items.Count >= 4)
            {
                _logger.LogInformation("Comparison list limit reached (4 items) when adding {SmartphoneId} | Session: {SessionId} | User: {UserId}",
                    smartphoneId, HttpContext.Session.Id, User.Identity?.Name ?? "Anonymous");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Ok(new { success = false, message = "Максимум 4 товара для сравнения", count = list.Items.Count });
                return RedirectToAction("Details", "Product", new { id = smartphoneId });
            }

            if (!list.Items.Any(i => i.SmartphoneId == smartphoneId))
            {
                var item = new Core.Entities.ComparisonItem
                {
                    ComparisonListId = list.Id,
                    SmartphoneId = smartphoneId
                };
                await _comparisonRepo.AddItemAsync(item);
                _logger.LogInformation("Added smartphone {SmartphoneId} to comparison list {ListId} | Session: {SessionId} | User: {UserId}",
                    smartphoneId, list.Id, HttpContext.Session.Id, User.Identity?.Name ?? "Anonymous");
            }

            var newCount = list.Items.Count + (list.Items.Any(i => i.SmartphoneId == smartphoneId) ? 0 : 1);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Ok(new { success = true, count = newCount });
            return RedirectToAction("Details", "Product", new { id = smartphoneId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding smartphone {SmartphoneId} to comparison | Session: {SessionId} | User: {UserId}",
                smartphoneId, HttpContext.Session.Id, User.Identity?.Name ?? "Anonymous");
            throw;
        }
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int itemId)
    {
        await _comparisonRepo.RemoveItemAsync(itemId);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Clear()
    {
        var list = await GetComparisonListAsync();
        await _comparisonRepo.ClearAsync(list.Id);
        return RedirectToAction("Index");
    }

    private async Task<Core.Entities.ComparisonList> GetComparisonListAsync()
    {
        var sessionId = HttpContext.Session.Id;
        Core.Entities.ComparisonList list = null;

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                list = await _comparisonRepo.GetByUserIdAsync(userId);
                
                if (list == null)
                {
                    var sessionList = await _comparisonRepo.GetBySessionIdAsync(sessionId);
                    if (sessionList != null)
                    {
                        sessionList.UserId = userId;
                        await _comparisonRepo.UpdateAsync(sessionList);
                        list = sessionList;
                    }
                }
                
                if (list != null) return list;
            }
        }

        list = await _comparisonRepo.GetBySessionIdAsync(sessionId);
        if (list != null) return list;

        var newList = new Core.Entities.ComparisonList
        {
            SessionId = sessionId,
            UserId = User.Identity?.IsAuthenticated == true
                ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                : null
        };
        await _comparisonRepo.AddAsync(newList);
        return newList;
    }
}