using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Web.Extensions;
using System.Text.Json;

namespace SmartphoneShop.Web.Controllers;

public class CartController : Controller
{
    private readonly ICartRepository _cartRepo;
    private readonly ISmartphoneRepository _smartphoneRepo;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartRepository cartRepo, ISmartphoneRepository smartphoneRepo, ILogger<CartController> logger)
    {
        _cartRepo = cartRepo;
        _smartphoneRepo = smartphoneRepo;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var cart = await GetCartAsync();
        
        var sessionCartItems = HttpContext.Session.GetObject<List<CartItemModel>>("CartItems");
        if (sessionCartItems != null && sessionCartItems.Any())
        {
            foreach (var sessionItem in sessionCartItems)
            {
                var smartphone = await _smartphoneRepo.GetByIdAsync(sessionItem.SmartphoneId);
                if (smartphone == null) continue;
                
                var existingItem = cart.Items.FirstOrDefault(i => i.SmartphoneId == sessionItem.SmartphoneId);
                if (existingItem != null)
                {
                    existingItem.Quantity += sessionItem.Quantity;
                    await _cartRepo.UpdateItemAsync(existingItem);
                }
                else
                {
                    var newItem = new Core.Entities.CartItem
                    {
                        CartId = cart.Id,
                        SmartphoneId = sessionItem.SmartphoneId,
                        Quantity = sessionItem.Quantity
                    };
                    await _cartRepo.AddItemAsync(newItem);
                }
            }
            HttpContext.Session.Remove("CartItems");
            cart = await GetCartAsync();
        }
        
        return View(cart);
    }

    [HttpGet]
    public async Task<IActionResult> Count()
    {
        var cart = await GetCartAsync();
        int count = cart.Items?.Sum(i => i.Quantity) ?? 0;
        return Json(new { count });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Add(int smartphoneId, int quantity = 1)
    {
        try
        {
            var smartphone = await _smartphoneRepo.GetByIdAsync(smartphoneId);
            if (smartphone == null)
            {
                _logger.LogWarning("Attempt to add non-existent smartphone {SmartphoneId} to cart | Session: {SessionId} | User: {UserId}", 
                    smartphoneId, HttpContext.Session.Id, User.Identity?.Name ?? "Anonymous");
                return NotFound();
            }

            if (smartphone.Quantity <= 0)
            {
                _logger.LogWarning("Attempt to add out-of-stock smartphone {SmartphoneId} to cart | Session: {SessionId} | User: {UserId}",
                    smartphoneId, HttpContext.Session.Id, User.Identity?.Name ?? "Anonymous");
                return BadRequest("Товара нет в наличии");
            }

            var cart = await GetCartAsync();
            var existingItem = cart.Items.FirstOrDefault(i => i.SmartphoneId == smartphoneId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                await _cartRepo.UpdateItemAsync(existingItem);
                _logger.LogInformation("Updated quantity for smartphone {SmartphoneId} in cart {CartId}, new quantity: {Quantity} | Session: {SessionId} | User: {UserId}",
                    smartphoneId, cart.Id, existingItem.Quantity, HttpContext.Session.Id, User.Identity?.Name ?? "Anonymous");
            }
            else
            {
                var newItem = new Core.Entities.CartItem
                {
                    CartId = cart.Id,
                    SmartphoneId = smartphoneId,
                    Quantity = quantity
                };
                await _cartRepo.AddItemAsync(newItem);
                _logger.LogInformation("Added smartphone {SmartphoneId} to cart {CartId}, quantity: {Quantity} | Session: {SessionId} | User: {UserId}",
                    smartphoneId, cart.Id, quantity, HttpContext.Session.Id, User.Identity?.Name ?? "Anonymous");
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok(new { success = true, message = "Товар добавлен в корзину", count = cart.Items.Sum(i => i.Quantity) });
            }

            return RedirectToAction("Details", "Product", new { id = smartphoneId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding smartphone {SmartphoneId} to cart | Session: {SessionId} | User: {UserId}",
                smartphoneId, HttpContext.Session.Id, User.Identity?.Name ?? "Anonymous");
            throw;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int itemId, int quantity)
    {
        var cart = await GetCartAsync();
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);

        if (item != null)
        {
            if (quantity <= 0)
            {
                await _cartRepo.RemoveItemAsync(itemId);
            }
            else
            {
                var smartphone = await _smartphoneRepo.GetByIdAsync(item.SmartphoneId);
                if (smartphone != null && quantity > smartphone.Quantity)
                {
                    TempData["Message"] = $"На складе осталось только {smartphone.Quantity} шт. — количество установлено на максимум";
                    quantity = smartphone.Quantity;
                }
                item.Quantity = quantity;
                await _cartRepo.UpdateItemAsync(item);
            }
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int itemId)
    {
        await _cartRepo.RemoveItemAsync(itemId);
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        var cart = await GetCartAsync();
        await _cartRepo.ClearAsync(cart.Id);
        return RedirectToAction("Index");
    }

    private async Task<Core.Entities.Cart> GetCartAsync()
    {
        var sessionId = HttpContext.Session.Id;
        Core.Entities.Cart cart = null;

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                cart = await _cartRepo.GetByUserIdAsync(userId);
                
                if (cart == null)
                {
                    var sessionCart = await _cartRepo.GetBySessionIdAsync(sessionId);
                    if (sessionCart != null)
                    {
                        sessionCart.UserId = userId;
                        await _cartRepo.UpdateAsync(sessionCart);
                        cart = sessionCart;
                    }
                }
                
                if (cart != null) return cart;
            }
        }

        cart = await _cartRepo.GetBySessionIdAsync(sessionId);
        if (cart != null) return cart;

        var newCart = new Core.Entities.Cart
        {
            SessionId = sessionId,
            UserId = User.Identity?.IsAuthenticated == true
                ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                : null
        };
        await _cartRepo.AddAsync(newCart);
        return newCart;
    }
}
