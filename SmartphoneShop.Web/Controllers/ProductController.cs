using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using SmartphoneShop.Web.Extensions;
using X.PagedList;

namespace SmartphoneShop.Web.Controllers;

public class ProductController : Controller
{
    private readonly ISmartphoneRepository _smartphoneRepo;
    private readonly IReviewRepository _reviewRepo;
    private readonly IFavoriteRepository _favoriteRepo;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<ProductController> _logger;

    public ProductController(ISmartphoneRepository smartphoneRepo, IReviewRepository reviewRepo, IFavoriteRepository favoriteRepo, UserManager<AppUser> userManager, ILogger<ProductController> logger)
    {
        _smartphoneRepo = smartphoneRepo;
        _reviewRepo = reviewRepo;
        _favoriteRepo = favoriteRepo;
        _userManager = userManager;
        _logger = logger;
    }

        public async Task<IActionResult> Details(int id, int reviewPage = 1, int reviewPageSize = 5)
        {
            reviewPage = Math.Max(1, reviewPage);
            var smartphone = await _smartphoneRepo.GetByIdAsync(id);
            if (smartphone == null) return NotFound();

            var reviews = await _reviewRepo.GetBySmartphoneIdPagedAsync(id, reviewPage, reviewPageSize);
            var avgRating = await _reviewRepo.GetAverageRatingAsync(id);

            bool isFavorite = false;
            bool userHasReview = false;
            int? userReviewId = null;
            string? userReviewComment = null;
            int? userReviewRating = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                isFavorite = await _favoriteRepo.ExistsAsync(user.Id, id);
                userHasReview = await _reviewRepo.UserHasReviewAsync(user.Id, id);
                if (userHasReview)
                {
                    var existing = (await _reviewRepo.GetBySmartphoneIdAsync(id))
                        .FirstOrDefault(r => r.UserId == user.Id);
                    if (existing != null)
                    {
                        userReviewId = existing.Id;
                        userReviewComment = existing.Comment;
                        userReviewRating = existing.Rating;
                    }
                }
            }

            ViewBag.Reviews = reviews;
            ViewBag.AvgRating = avgRating;
            ViewBag.IsFavorite = isFavorite;
            ViewBag.UserHasReview = userHasReview;
            ViewBag.UserReviewId = userReviewId;
            ViewBag.UserReviewComment = userReviewComment;
            ViewBag.UserReviewRating = userReviewRating;

            return View(smartphone);
        }

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> AddReview(int smartphoneId, int rating, string comment)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return RedirectToAction("Login", "Account");

        if (string.IsNullOrEmpty(comment))
        {
            TempData["Error"] = "Напишите комментарий";
            return RedirectToAction("Details", new { id = smartphoneId });
        }

        if (await _reviewRepo.UserHasReviewAsync(userId, smartphoneId))
        {
            TempData["Error"] = "Вы уже оставили отзыв на этот товар";
            return RedirectToAction("Details", new { id = smartphoneId });
        }

        var review = new Review
        {
            SmartphoneId = smartphoneId,
            UserId = userId,
            Rating = rating,
            Comment = comment
        };

        await _reviewRepo.AddAsync(review);
        return RedirectToAction("Details", new { id = smartphoneId });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UpdateReview(int reviewId, int rating, string comment)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return RedirectToAction("Login", "Account");

        var review = await _reviewRepo.GetByIdAsync(reviewId);
        if (review == null || review.UserId != userId) return NotFound();

        if (string.IsNullOrEmpty(comment))
        {
            TempData["Error"] = "Напишите комментарий";
            return RedirectToAction("Details", new { id = review.SmartphoneId });
        }

        review.Rating = rating;
        review.Comment = comment;

        await _reviewRepo.UpdateAsync(review);
        return RedirectToAction("Details", new { id = review.SmartphoneId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> BuyNow(int id)
    {
        var smartphone = await _smartphoneRepo.GetByIdAsync(id);
        if (smartphone == null) return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Challenge();

        HttpContext.Session.SetInt32("BuyNowSmartphoneId", smartphone.Id);
        HttpContext.Session.SetString("BuyNowSmartphoneName", smartphone.ModelName);
        HttpContext.Session.SetDecimal("BuyNowPrice", smartphone.Price);

        return RedirectToAction("Checkout", "Order");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> AddToCart(int id, int quantity = 1)
    {
        var smartphone = await _smartphoneRepo.GetByIdAsync(id);
        if (smartphone == null) return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var cartItems = HttpContext.Session.GetObject<List<CartItemModel>>("CartItems") ?? new List<CartItemModel>();
        
        var existing = cartItems.FirstOrDefault(x => x.SmartphoneId == id);
        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            cartItems.Add(new CartItemModel
            {
                SmartphoneId = smartphone.Id,
                ModelName = smartphone.ModelName,
                Price = smartphone.Price,
                Quantity = quantity
            });
        }
        
        HttpContext.Session.SetObject("CartItems", cartItems);
        
        return RedirectToAction("Index", "Cart");
    }
}
