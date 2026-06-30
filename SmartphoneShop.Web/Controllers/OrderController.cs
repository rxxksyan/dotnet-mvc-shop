using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Enums;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Web.Extensions;
using System.Security.Claims;
using X.PagedList;

namespace SmartphoneShop.Web.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize]
public class OrderController : Controller
{
    private readonly ICartRepository _cartRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly ISmartphoneRepository _smartphoneRepo;
    private readonly UserManager<AppUser> _userManager;

    public OrderController(ICartRepository cartRepo, IOrderRepository orderRepo, ISmartphoneRepository smartphoneRepo, UserManager<AppUser> userManager)
    {
        _cartRepo = cartRepo;
        _orderRepo = orderRepo;
        _smartphoneRepo = smartphoneRepo;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return RedirectToAction("Login", "Account");

        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            ViewBag.UserFullName = user.FullName;
            ViewBag.UserPhone = user.Phone;
        }

        // Проверяем BuyNow из сессии
        var buyNowId = HttpContext.Session.GetInt32("BuyNowSmartphoneId");
        if (buyNowId.HasValue)
        {
            var smartphone = await _smartphoneRepo.GetByIdAsync(buyNowId.Value);
            if (smartphone != null)
            {
                ViewBag.IsBuyNow = true;
                ViewBag.BuyNowItem = new CartItemModel
                {
                    SmartphoneId = smartphone.Id,
                    ModelName = smartphone.ModelName,
                    Price = smartphone.Price,
                    Quantity = 1,
                    ImageUrl = smartphone.ImageUrl
                };
                return View();
            }
        }

        var cart = await _cartRepo.GetByUserIdAsync(userId);
        if (cart == null || !cart.Items.Any())
        {
            TempData["Error"] = "Корзина пуста";
            return RedirectToAction("Index", "Cart");
        }

        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string deliveryType, string deliveryAddress, string contactPhone, string contactName, string? notes)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return RedirectToAction("Login", "Account");

        var dt = deliveryType == "Pickup" ? DeliveryType.Pickup : DeliveryType.Delivery;

        if (dt == DeliveryType.Delivery && string.IsNullOrWhiteSpace(deliveryAddress))
        {
            TempData["Error"] = "Укажите адрес доставки";
            return RedirectToAction("Checkout");
        }
        if (string.IsNullOrWhiteSpace(contactPhone))
        {
            TempData["Error"] = "Укажите контактный телефон";
            return RedirectToAction("Checkout");
        }
        if (string.IsNullOrWhiteSpace(contactName))
        {
            TempData["Error"] = "Укажите контактное имя";
            return RedirectToAction("Checkout");
        }

        Order order;
        var smartphonesToDecrement = new List<(int smartphoneId, int quantity)>();

        var buyNowId = HttpContext.Session.GetInt32("BuyNowSmartphoneId");
        if (buyNowId.HasValue)
        {
            var smartphone = await _smartphoneRepo.GetByIdAsync(buyNowId.Value);
            if (smartphone == null) return NotFound();

            if (smartphone.Quantity < 1)
            {
                TempData["Error"] = "Товара нет в наличии";
                return RedirectToAction("Checkout");
            }

            smartphonesToDecrement.Add((smartphone.Id, 1));

            order = new Order
            {
                UserId = userId,
                TotalAmount = smartphone.Price,
                Status = OrderStatus.Pending,
                DeliveryType = dt,
                DeliveryAddress = dt == DeliveryType.Pickup ? "Самовывоз (магазин)" : deliveryAddress,
                ContactPhone = contactPhone,
                ContactName = contactName,
                Notes = notes,
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        SmartphoneId = smartphone.Id,
                        Quantity = 1,
                        PriceAtPurchase = smartphone.Price
                    }
                }
            };

            HttpContext.Session.Remove("BuyNowSmartphoneId");
            HttpContext.Session.Remove("BuyNowSmartphoneName");
            HttpContext.Session.Remove("BuyNowPrice");
        }
        else
        {
            var cart = await _cartRepo.GetByUserIdAsync(userId);
            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "Корзина пуста";
                return RedirectToAction("Index", "Cart");
            }

            foreach (var item in cart.Items)
            {
                var smartphone = await _smartphoneRepo.GetByIdAsync(item.SmartphoneId);
                if (smartphone == null)
                {
                    TempData["Error"] = "Один из товаров недоступен";
                    return RedirectToAction("Checkout");
                }
                if (smartphone.Quantity < item.Quantity)
                {
                    TempData["Error"] = $"Недостаточно товара \"{smartphone.ModelName}\" на складе (доступно: {smartphone.Quantity} шт.)";
                    return RedirectToAction("Checkout");
                }
                smartphonesToDecrement.Add((smartphone.Id, item.Quantity));
            }

            order = new Order
            {
                UserId = userId,
                TotalAmount = cart.TotalAmount,
                Status = OrderStatus.Pending,
                DeliveryType = dt,
                DeliveryAddress = dt == DeliveryType.Pickup ? "Самовывоз (магазин)" : deliveryAddress,
                ContactPhone = contactPhone,
                ContactName = contactName,
                Notes = notes,
                Items = cart.Items.Select(i => new OrderItem
                {
                    SmartphoneId = i.SmartphoneId,
                    Quantity = i.Quantity,
                    PriceAtPurchase = i.Smartphone.Price
                }).ToList()
            };

            await _cartRepo.ClearAsync(cart.Id);
        }

        await _orderRepo.AddAsync(order);

        foreach (var (smartphoneId, quantity) in smartphonesToDecrement)
        {
            var smartphone = await _smartphoneRepo.GetByIdAsync(smartphoneId);
            if (smartphone != null)
            {
                smartphone.Quantity -= quantity;
                await _smartphoneRepo.UpdateAsync(smartphone);
            }
        }

        return RedirectToAction("Success", new { id = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Success(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var order = await _orderRepo.GetByIdAsync(id);

        if (order == null || order.UserId != userId)
            return NotFound();

        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> History(int page = 1, int pageSize = 10)
    {
        page = Math.Max(1, page);
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return RedirectToAction("Login", "Account");

        var orders = await _orderRepo.GetByUserIdPagedAsync(userId, page, pageSize);
        return View(orders);
    }
}
