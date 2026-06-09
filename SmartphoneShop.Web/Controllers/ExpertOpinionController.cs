using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Infrastructure.Data;

namespace SmartphoneShop.Web.Controllers;

public class ExpertOpinionController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public ExpertOpinionController(AppDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int smartphoneId1, int smartphoneId2, string text, string? videoUrl)
    {
        if (string.IsNullOrEmpty(text))
        {
            return RedirectToAction("Index", "Comparison");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Account");

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains("Expert") && !roles.Contains("Admin") && !roles.Contains("Народный эксперт"))
        {
            return RedirectToAction("Index", "Comparison");
        }

        var id1 = Math.Min(smartphoneId1, smartphoneId2);
        var id2 = Math.Max(smartphoneId1, smartphoneId2);

        var existingOpinion = await _context.ExpertOpinions
            .FirstOrDefaultAsync(o => o.SmartphoneId1 == id1 && o.SmartphoneId2 == id2 && o.ExpertId == user.Id);

        if (existingOpinion != null)
        {
            return RedirectToAction("Index", "Comparison");
        }

        var opinion = new ExpertOpinion
        {
            SmartphoneId1 = id1,
            SmartphoneId2 = id2,
            Text = text,
            VideoUrl = videoUrl,
            ExpertId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.ExpertOpinions.Add(opinion);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Comparison");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var opinion = await _context.ExpertOpinions.FindAsync(id);
        if (opinion == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        if (opinion.ExpertId != user.Id && !roles.Contains("Admin") && !roles.Contains("Expert"))
        {
            return RedirectToAction("Index", "Comparison");
        }

        return View(opinion);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string text, string? videoUrl)
    {
        if (string.IsNullOrEmpty(text))
        {
            return RedirectToAction("Edit", new { id });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var opinion = await _context.ExpertOpinions.FindAsync(id);
        if (opinion == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        if (opinion.ExpertId != user.Id && !roles.Contains("Admin") && !roles.Contains("Expert"))
        {
            return RedirectToAction("Index", "Comparison");
        }

        opinion.Text = text;
        opinion.VideoUrl = videoUrl;
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Comparison");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var opinion = await _context.ExpertOpinions.FindAsync(id);
        if (opinion == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        if (opinion.ExpertId != user.Id && !roles.Contains("Admin") && !roles.Contains("Expert"))
        {
            return RedirectToAction("Index", "Comparison");
        }

        _context.ExpertOpinions.Remove(opinion);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Comparison");
    }
}