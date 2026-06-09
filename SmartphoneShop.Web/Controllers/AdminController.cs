using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Infrastructure.Data;

namespace SmartphoneShop.Web.Controllers;

[Authorize(Roles = "Admin,ProductAdmin,RepairSpecialist,Expert")]
public class AdminController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDbContext _context;

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
        ViewBag.PurchaseOrdersCount = await _context.PurchaseOrders.CountAsync(po => po.Status == "Pending");
        return View();
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users()
    {
        var users = _userManager.Users
            .Include(u => u.Orders)
            .Include(u => u.RepairRequests)
            .Include(u => u.Reviews)
            .ToList();

        var userRoles = new Dictionary<string, IList<string>>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRoles[user.Id] = roles;
        }

        ViewBag.UserRoles = userRoles;
        ViewBag.AvailableRoles = new[] { "Expert", "ProductAdmin", "RepairSpecialist", "Admin", "Народный эксперт" };
        
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(role))
        {
            await _userManager.AddToRoleAsync(user, role);
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRole(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        if (role == "User")
        {
            TempData["Error"] = "Нельзя удалить базовую роль Пользователь";
            return RedirectToAction(nameof(Users));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Contains(role))
        {
            await _userManager.RemoveFromRoleAsync(user, role);
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
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
    }}