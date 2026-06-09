using Microsoft.AspNetCore.Mvc;
using SmartphoneShop.Core.Interfaces;

namespace SmartphoneShop.Web.Controllers;

public class FavoritesController : Controller
{
    private readonly IFavoriteRepository _favoriteRepo;
    private readonly ISmartphoneRepository _smartphoneRepo;

    public FavoritesController(IFavoriteRepository favoriteRepo, ISmartphoneRepository smartphoneRepo)
    {
        _favoriteRepo = favoriteRepo;
        _smartphoneRepo = smartphoneRepo;
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return RedirectToAction("Login", "Account");

        var favorites = await _favoriteRepo.GetByUserIdAsync(userId);
        return View(favorites);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Toggle(int smartphoneId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return RedirectToAction("Login", "Account");

        var smartphone = await _smartphoneRepo.GetByIdAsync(smartphoneId);
        if (smartphone == null) return NotFound();

        var exists = await _favoriteRepo.ExistsAsync(userId, smartphoneId);

        if (exists)
        {
            var favorite = await _favoriteRepo.GetByUserAndSmartphoneAsync(userId, smartphoneId);
            if (favorite != null)
            {
                await _favoriteRepo.DeleteAsync(favorite.Id);
            }
        }
        else
        {
            var favorite = new Core.Entities.Favorite
            {
                UserId = userId,
                SmartphoneId = smartphoneId
            };
            await _favoriteRepo.AddAsync(favorite);
        }

        return Ok();
    }

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Remove(int id)
    {
        await _favoriteRepo.DeleteAsync(id);
        return RedirectToAction("Index");
    }
}