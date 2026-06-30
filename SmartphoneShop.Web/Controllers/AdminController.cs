using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Infrastructure.Data;
using X.PagedList;

namespace SmartphoneShop.Web.Controllers;

[Authorize(Roles = "Admin,ProductAdmin,RepairSpecialist")]
public class AdminController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDbContext _context;

    private static readonly string[] AvailableRoles = { "User", "Народный эксперт", "ProductAdmin", "RepairSpecialist", "Admin" };

    public AdminController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, AppDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.UsersCount = await _userManager.Users.CountAsync();
        ViewBag.OrdersCount = await _context.Orders.CountAsync();
        ViewBag.SmartphonesCount = await _context.Smartphones.CountAsync();
        ViewBag.RepairsCount = await _context.RepairRequests.CountAsync();
        ViewBag.SparePartsCount = await _context.SpareParts.CountAsync();
        ViewBag.PurchaseOrdersCount = await _context.PurchaseOrders.CountAsync(po => po.Status == "Pending");
        return View();
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users(string? search, int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page);
        var query = _userManager.Users
            .Include(u => u.Orders)
            .Include(u => u.RepairRequests)
            .Include(u => u.Reviews)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowerSearch = search.ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(lowerSearch) ||
                                     u.Email.ToLower().Contains(lowerSearch));
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .ToPagedListAsync(page, pageSize);

        var userRoles = new Dictionary<string, IList<string>>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRoles[user.Id] = roles;
        }

        ViewBag.UserRoles = userRoles;
        ViewBag.AvailableRoles = AvailableRoles;
        ViewBag.Search = search;

        return View(users);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult CreateUser()
    {
        ViewBag.AvailableRoles = AvailableRoles;
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(string fullName, string email, string? phone, string password, string role)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            TempData["Error"] = "Заполните обязательные поля";
            ViewBag.AvailableRoles = AvailableRoles;
            return View();
        }

        if (!AvailableRoles.Contains(role))
        {
            TempData["Error"] = "Выбрана недопустимая роль";
            ViewBag.AvailableRoles = AvailableRoles;
            return View();
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            Phone = phone,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, role);
            return RedirectToAction(nameof(Users));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
        ViewBag.AvailableRoles = AvailableRoles;
        return View();
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EditUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.CurrentRole = roles.FirstOrDefault() ?? "User";
        ViewBag.AvailableRoles = AvailableRoles;
        ViewBag.UserId = id;

        return View(user);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(string id, string fullName, string email, string? phone, string role, string? newPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Заполните обязательные поля";
            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.CurrentRole = roles.FirstOrDefault() ?? "User";
            ViewBag.AvailableRoles = AvailableRoles;
            ViewBag.UserId = id;
            return View(user);
        }

        if (!AvailableRoles.Contains(role))
        {
            TempData["Error"] = "Выбрана недопустимая роль";
            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.CurrentRole = roles.FirstOrDefault() ?? "User";
            ViewBag.AvailableRoles = AvailableRoles;
            ViewBag.UserId = id;
            return View(user);
        }

        user.FullName = fullName;
        user.Email = email;
        user.UserName = email;
        user.Phone = phone;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            TempData["Error"] = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            var currentRoles = await _userManager.GetRolesAsync(user);
            ViewBag.CurrentRole = currentRoles.FirstOrDefault() ?? "User";
            ViewBag.AvailableRoles = AvailableRoles;
            ViewBag.UserId = id;
            return View(user);
        }

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passResult = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!passResult.Succeeded)
            {
                TempData["Error"] = "Ошибка смены пароля: " + string.Join("; ", passResult.Errors.Select(e => e.Description));
                var currentRoles2 = await _userManager.GetRolesAsync(user);
                ViewBag.CurrentRole = currentRoles2.FirstOrDefault() ?? "User";
                ViewBag.AvailableRoles = AvailableRoles;
                ViewBag.UserId = id;
                return View(user);
            }
        }

        var currentRolesFinal = await _userManager.GetRolesAsync(user);
        if (!currentRolesFinal.Contains(role))
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));

            await _userManager.RemoveFromRolesAsync(user, currentRolesFinal);
            await _userManager.AddToRoleAsync(user, role);
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        if (userId == currentUserId)
        {
            TempData["Error"] = "Нельзя удалить самого себя";
            return RedirectToAction(nameof(Users));
        }

        await _userManager.DeleteAsync(user);
        return RedirectToAction(nameof(Users));
    }
}
