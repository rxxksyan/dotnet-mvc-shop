using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Web.Controllers;
using SmartphoneShop.Web.Extensions;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace SmartphoneShop.Tests.Controllers;

public class CartControllerTests
{
    private readonly Mock<ICartRepository> _cartRepo;
    private readonly Mock<ISmartphoneRepository> _smartphoneRepo;
    private readonly Mock<ILogger<CartController>> _logger;
    private readonly CartController _controller;
    private readonly Mock<ISession> _session;
    private readonly DefaultHttpContext _httpContext;

    public CartControllerTests()
    {
        _cartRepo = new Mock<ICartRepository>();
        _smartphoneRepo = new Mock<ISmartphoneRepository>();
        _logger = new Mock<ILogger<CartController>>();
        _session = new Mock<ISession>();

        _controller = new CartController(_cartRepo.Object, _smartphoneRepo.Object, _logger.Object);

        _httpContext = new DefaultHttpContext
        {
            Session = _session.Object
        };
        var sessionId = "test-session-id";
        _session.Setup(s => s.Id).Returns(sessionId);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _httpContext
        };
    }

    private Cart CreateTestCart()
    {
        var cart = new Cart
        {
            Id = 1,
            SessionId = "test-session-id",
            Items = new List<CartItem>
            {
                new() { Id = 1, CartId = 1, SmartphoneId = 1, Quantity = 2,
                    Smartphone = new Smartphone { Id = 1, ModelName = "Galaxy", Price = 100 } }
            }
        };
        return cart;
    }

    [Fact]
    public async Task Index_ReturnsViewWithCart()
    {
        var cart = CreateTestCart();
        _cartRepo.Setup(r => r.GetBySessionIdAsync("test-session-id")).ReturnsAsync(cart);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Cart>(viewResult.ViewData.Model);
        Assert.Equal(1, model.Id);
    }

    [Fact]
    public async Task Index_CreatesNewCart_WhenNoneExists()
    {
        _cartRepo.Setup(r => r.GetBySessionIdAsync("test-session-id")).ReturnsAsync((Cart?)null);
        _cartRepo.Setup(r => r.AddAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        _cartRepo.Verify(r => r.AddAsync(It.IsAny<Cart>()), Times.Once);
    }

    [Fact]
    public async Task Count_ReturnsJsonWithCount()
    {
        var cart = CreateTestCart();
        _cartRepo.Setup(r => r.GetBySessionIdAsync("test-session-id")).ReturnsAsync(cart);

        var result = await _controller.Count();

        var jsonResult = Assert.IsType<JsonResult>(result);
        var value = jsonResult.Value;
        var count = value?.GetType().GetProperty("count")?.GetValue(value);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Add_WithValidSmartphone_AddsToCart()
    {
        var cart = CreateTestCart();
        cart.Items.Clear();
        _cartRepo.Setup(r => r.GetBySessionIdAsync("test-session-id")).ReturnsAsync(cart);
        _smartphoneRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Smartphone { Id = 1, Quantity = 10, Price = 100 });

        var result = await _controller.Add(1, 1);

        Assert.IsType<RedirectToActionResult>(result);
        _cartRepo.Verify(r => r.AddItemAsync(It.IsAny<CartItem>()), Times.Once);
    }

    [Fact]
    public async Task Add_WithNonExistentSmartphone_ReturnsNotFound()
    {
        _smartphoneRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Smartphone?)null);

        var result = await _controller.Add(999, 1);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Add_WithExistingItem_IncrementsQuantity()
    {
        var cart = CreateTestCart();
        _cartRepo.Setup(r => r.GetBySessionIdAsync("test-session-id")).ReturnsAsync(cart);
        _smartphoneRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Smartphone { Id = 1, Quantity = 10, Price = 100 });

        var result = await _controller.Add(1, 3);

        Assert.IsType<RedirectToActionResult>(result);
        _cartRepo.Verify(r => r.UpdateItemAsync(It.Is<CartItem>(i => i.Quantity == 5)), Times.Once);
    }

    [Fact]
    public async Task Add_WithAjaxRequest_ReturnsJson()
    {
        var cart = CreateTestCart();
        _cartRepo.Setup(r => r.GetBySessionIdAsync("test-session-id")).ReturnsAsync(cart);
        _smartphoneRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Smartphone { Id = 1, Quantity = 10, Price = 100 });

        _httpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

        var result = await _controller.Add(1, 1);

        var jsonResult = Assert.IsType<OkObjectResult>(result);
        var value = jsonResult.Value;
        var success = value?.GetType().GetProperty("success")?.GetValue(value);
        Assert.True((bool)success!);
    }

    [Fact]
    public async Task Update_WithPositiveQuantity_UpdatesItem()
    {
        var cart = CreateTestCart();
        _cartRepo.Setup(r => r.GetBySessionIdAsync("test-session-id")).ReturnsAsync(cart);

        var result = await _controller.Update(1, 5);

        Assert.IsType<RedirectToActionResult>(result);
        _cartRepo.Verify(r => r.UpdateItemAsync(It.Is<CartItem>(i => i.Quantity == 5)), Times.Once);
    }

    [Fact]
    public async Task Update_WithZeroQuantity_RemovesItem()
    {
        var cart = CreateTestCart();
        _cartRepo.Setup(r => r.GetBySessionIdAsync("test-session-id")).ReturnsAsync(cart);

        var result = await _controller.Update(1, 0);

        Assert.IsType<RedirectToActionResult>(result);
        _cartRepo.Verify(r => r.RemoveItemAsync(1), Times.Once);
    }

    [Fact]
    public async Task Remove_RemovesItem()
    {
        var result = await _controller.Remove(1);

        Assert.IsType<RedirectToActionResult>(result);
        _cartRepo.Verify(r => r.RemoveItemAsync(1), Times.Once);
    }

    [Fact]
    public async Task Clear_ClearsCart()
    {
        var cart = CreateTestCart();
        _cartRepo.Setup(r => r.GetBySessionIdAsync("test-session-id")).ReturnsAsync(cart);

        var result = await _controller.Clear();

        Assert.IsType<RedirectToActionResult>(result);
        _cartRepo.Verify(r => r.ClearAsync(cart.Id), Times.Once);
    }
}
