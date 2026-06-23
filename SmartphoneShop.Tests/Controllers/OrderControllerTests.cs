using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Enums;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Web.Controllers;
using SmartphoneShop.Web.Extensions;
using System.Security.Claims;
using X.PagedList;
using Xunit;

namespace SmartphoneShop.Tests.Controllers;

public class OrderControllerTests
{
    private readonly Mock<ICartRepository> _cartRepo;
    private readonly Mock<IOrderRepository> _orderRepo;
    private readonly Mock<ISmartphoneRepository> _smartphoneRepo;
    private readonly Mock<UserManager<AppUser>> _userManager;
    private readonly OrderController _controller;
    private readonly TestSession _session;
    private readonly DefaultHttpContext _httpContext;
    private readonly string _userId = "user1";

    public OrderControllerTests()
    {
        _cartRepo = new Mock<ICartRepository>();
        _orderRepo = new Mock<IOrderRepository>();
        _smartphoneRepo = new Mock<ISmartphoneRepository>();

        var store = new Mock<IUserStore<AppUser>>();
        _userManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
        _session = new TestSession();

        _controller = new OrderController(_cartRepo.Object, _orderRepo.Object, _smartphoneRepo.Object, _userManager.Object);

        _httpContext = new DefaultHttpContext { Session = _session };

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _userId)
        }, "TestAuth"));

        _httpContext.User = claimsPrincipal;
        _controller.ControllerContext = new ControllerContext { HttpContext = _httpContext };

        var tempDataProvider = Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>();
        _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(), tempDataProvider);
    }

    [Fact]
    public async Task Checkout_ReturnsView()
    {
        var cart = new Cart
        {
            Id = 1,
            UserId = _userId,
            Items = new List<CartItem>
            {
                new() { SmartphoneId = 1, Quantity = 1, Smartphone = new Smartphone { Id = 1, Price = 100 } }
            }
        };
        _cartRepo.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync(cart);
        _userManager.Setup(u => u.FindByIdAsync(_userId)).ReturnsAsync(new AppUser { Id = _userId, FullName = "John", Phone = "123" });

        var result = await _controller.Checkout();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("John", viewResult.ViewData["UserFullName"]);
        Assert.Equal("123", viewResult.ViewData["UserPhone"]);
    }

    [Fact]
    public async Task Checkout_WithEmptyCart_RedirectsToCart()
    {
        _cartRepo.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync((Cart?)null);
        _userManager.Setup(u => u.FindByIdAsync(_userId)).ReturnsAsync(new AppUser { Id = _userId });

        var result = await _controller.Checkout();

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Cart", redirectResult.ControllerName);
    }

    [Fact]
    public async Task Create_WithValidData_CreatesOrder()
    {
        var cart = new Cart
        {
            Id = 1,
            UserId = _userId,
            Items = new List<CartItem>
            {
                new() { SmartphoneId = 1, Quantity = 2, Smartphone = new Smartphone { Id = 1, Price = 100, ModelName = "Galaxy" } }
            }
        };
        _cartRepo.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync(cart);

        var result = await _controller.Create("Address 123", "1234567890", "John", null);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Success", redirectResult.ActionName);
        _orderRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
        _cartRepo.Verify(r => r.ClearAsync(cart.Id), Times.Once);
    }

    [Fact]
    public async Task Create_WithMissingFields_ReturnsError()
    {
        var result = await _controller.Create("", "", "", null);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Checkout", redirectResult.ActionName);
        Assert.Equal("Укажите адрес доставки", _controller.TempData["Error"]);
    }

    [Fact]
    public async Task Success_WithOwnOrder_ReturnsView()
    {
        var order = new Order { Id = 1, UserId = _userId };
        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var result = await _controller.Success(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Order>(viewResult.ViewData.Model);
        Assert.Equal(1, model.Id);
    }

    [Fact]
    public async Task Success_WithWrongUser_ReturnsNotFound()
    {
        var order = new Order { Id = 1, UserId = "other-user" };
        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var result = await _controller.Success(1);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task History_ReturnsViewWithPagedOrders()
    {
        var orders = new List<Order> { new() { Id = 1, UserId = _userId } };
        var paged = new StaticPagedList<Order>(orders, 1, 10, 1);
        _orderRepo.Setup(r => r.GetByUserIdPagedAsync(_userId, 1, 10)).ReturnsAsync(paged);

        var result = await _controller.History(1, 10);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IPagedList<Order>>(viewResult.ViewData.Model);
        Assert.Single(model);
    }
}

public class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _data = new();

    public string Id => "test-session-id";
    public bool IsAvailable => true;
    public IEnumerable<string> Keys => _data.Keys;

    public void Clear() => _data.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _data.Remove(key);
    public void Set(string key, byte[]? value)
    {
        if (value != null) _data[key] = value; else _data.Remove(key);
    }
    public bool TryGetValue(string key, out byte[]? value) => _data.TryGetValue(key, out value);
}
