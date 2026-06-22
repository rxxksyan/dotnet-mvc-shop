using Microsoft.AspNetCore.Mvc;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Web.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace SmartphoneShop.Web.Controllers;

public class HomeController : Controller
{
    private readonly ISmartphoneRepository _smartphoneRepo;
    private readonly IFavoriteRepository _favoriteRepo;
    private readonly IReviewRepository _reviewRepo;

    public HomeController(ISmartphoneRepository smartphoneRepo, IFavoriteRepository favoriteRepo, IReviewRepository reviewRepo)
    {
        _smartphoneRepo = smartphoneRepo;
        _favoriteRepo = favoriteRepo;
        _reviewRepo = reviewRepo;
    }

    public async Task<IActionResult> Index()
    {
        var featured = (await _smartphoneRepo.GetFeaturedAsync()).ToList();
        var ratings = new Dictionary<int, double>();
        foreach (var phone in featured)
        {
            ratings[phone.Id] = await _reviewRepo.GetAverageRatingAsync(phone.Id);
        }
        ViewBag.Ratings = ratings;

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var favorites = await _favoriteRepo.GetByUserIdAsync(userId);
            ViewBag.FavoriteIds = new HashSet<int>(favorites.Select(f => f.SmartphoneId));
        }
        else
        {
            ViewBag.FavoriteIds = new HashSet<int>();
        }

        var sessionId = HttpContext.Session.Id;
        var userIdComp = User.Identity?.IsAuthenticated == true
            ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            : null;

        var comparisonRepo = HttpContext.RequestServices.GetRequiredService<IComparisonRepository>();
        SmartphoneShop.Core.Entities.ComparisonList? comparisonList = null;

        if (userIdComp != null)
        {
            comparisonList = await comparisonRepo.GetByUserIdAsync(userIdComp);
        }
        if (comparisonList == null)
        {
            comparisonList = await comparisonRepo.GetBySessionIdAsync(sessionId);
        }
        if (comparisonList != null)
        {
            ViewBag.ComparisonIds = new HashSet<int>(comparisonList.Items.Select(i => i.SmartphoneId));
        }
        else
        {
            ViewBag.ComparisonIds = new HashSet<int>();
        }

        return View(featured);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode)
    {
        return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}