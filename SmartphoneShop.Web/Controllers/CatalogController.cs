using Microsoft.AspNetCore.Mvc;
using SmartphoneShop.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using SmartphoneShop.Core.Entities;
using X.PagedList;

namespace SmartphoneShop.Web.Controllers;

public class CatalogController : Controller
{
    private readonly ISmartphoneRepository _smartphoneRepo;
    private readonly IFavoriteRepository _favoriteRepo;
    private readonly IReviewRepository _reviewRepo;
    private readonly UserManager<AppUser> _userManager;

    public CatalogController(ISmartphoneRepository smartphoneRepo, IFavoriteRepository favoriteRepo, IReviewRepository reviewRepo, UserManager<AppUser> userManager)
    {
        _smartphoneRepo = smartphoneRepo;
        _favoriteRepo = favoriteRepo;
        _reviewRepo = reviewRepo;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? brand, decimal? minPrice, decimal? maxPrice, int? ram, int? storage, string? sort, string? search, int page = 1, int pageSize = 12)
    {
        page = Math.Max(1, page);
        var pagedSmartphones = await _smartphoneRepo.GetFilteredPagedAsync(brand, minPrice, maxPrice, ram, storage, sort, search, page, pageSize);
        var ratings = new Dictionary<int, double>();
        foreach (var phone in pagedSmartphones)
        {
            ratings[phone.Id] = await _reviewRepo.GetAverageRatingAsync(phone.Id);
        }
        ViewBag.Ratings = ratings;

        HashSet<int> favoriteIds = new();
        HashSet<int> comparisonIds = new();

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            var favorites = await _favoriteRepo.GetByUserIdAsync(user.Id);
            favoriteIds = new HashSet<int>(favorites.Select(f => f.SmartphoneId));
        }

        var sessionId = HttpContext.Session.Id;
        var userId = User.Identity?.IsAuthenticated == true
            ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            : null;

        var comparisonRepo = HttpContext.RequestServices.GetRequiredService<IComparisonRepository>();
        SmartphoneShop.Core.Entities.ComparisonList? comparisonList = null;

        if (userId != null)
        {
            comparisonList = await comparisonRepo.GetByUserIdAsync(userId);
        }
        if (comparisonList == null)
        {
            comparisonList = await comparisonRepo.GetBySessionIdAsync(sessionId);
        }
        if (comparisonList != null)
        {
            comparisonIds = new HashSet<int>(comparisonList.Items.Select(i => i.SmartphoneId));
        }

        ViewBag.FavoriteIds = favoriteIds;
        ViewBag.ComparisonIds = comparisonIds;
        ViewBag.Brands = new[] { "Samsung", "Apple", "Xiaomi", "Google" };
        ViewBag.CurrentBrand = brand;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.Ram = ram;
        ViewBag.Storage = storage;
        ViewBag.Sort = sort;
        ViewBag.Search = search;

        return View(pagedSmartphones);
    }
}
