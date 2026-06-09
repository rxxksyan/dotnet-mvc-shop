using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;

namespace SmartphoneShop.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            TempData["Error"] = "Заполните все поля";
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            TempData["Error"] = "Неверный email или пароль";
            return View();
        }

        var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        TempData["Error"] = "Неверный email или пароль";
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string fullName, string email, string phoneNumber, string password, string confirmPassword)
    {
        if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(password))
        {
            TempData["Error"] = "Заполните все поля";
            return View();
        }

        if (password != confirmPassword)
        {
            TempData["Error"] = "Пароли не совпадают";
            return View();
        }

        if (password.Length < 6)
        {
            TempData["Error"] = "Пароль должен быть не менее 6 символов";
            return View();
        }

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            TempData["Error"] = "Пользователь с таким email уже существует";
            return View();
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            Phone = phoneNumber
        };

        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
            await _signInManager.SignInAsync(user, false);
            return RedirectToAction("Index", "Home");
        }

        TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.Users
            .Include(u => u.Orders)
                .ThenInclude(o => o.Items)
                    .ThenInclude(i => i.Smartphone)
            .Include(u => u.RepairRequests)
            .Include(u => u.Reviews)
                .ThenInclude(r => r.Smartphone)
            .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));
        
        if (user == null)
            return Challenge();

        return View(user);
    }

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateProfile(string fullName, string phone)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new { success = false, message = "Пользователь не найден" });

        if (!string.IsNullOrEmpty(fullName))
            user.FullName = fullName;
        
        if (!string.IsNullOrEmpty(phone))
            user.Phone = phone;

        var result = await _userManager.UpdateAsync(user);
        
        if (result.Succeeded)
        {
            return Json(new { success = true });
        }
        else
        {
            return Json(new { success = false, message = "Ошибка при обновлении данных" });
        }
    }

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        if (string.IsNullOrEmpty(currentPassword))
        {
            TempData["Error"] = "Введите текущий пароль";
            return RedirectToAction(nameof(Profile));
        }

        if (string.IsNullOrEmpty(newPassword))
        {
            TempData["Error"] = "Введите новый пароль";
            return RedirectToAction(nameof(Profile));
        }

        if (newPassword != confirmPassword)
        {
            TempData["Error"] = "Пароли не совпадают";
            return RedirectToAction(nameof(Profile));
        }

        if (newPassword.Length < 6)
        {
            TempData["Error"] = "Новый пароль должен быть не менее 6 символов";
            return RedirectToAction(nameof(Profile));
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        
        if (result.Succeeded)
        {
            TempData["Success"] = "Пароль успешно изменён";
        }
        else
        {
            TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Profile));
    }
}