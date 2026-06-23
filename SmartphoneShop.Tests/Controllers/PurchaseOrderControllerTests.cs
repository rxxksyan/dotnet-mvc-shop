using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Web.Controllers;
using System.Security.Claims;
using X.PagedList;
using Xunit;

namespace SmartphoneShop.Tests.Controllers;

public class PurchaseOrderControllerTests
{
    private readonly Mock<IPurchaseOrderRepository> _purchaseOrderRepo;
    private readonly Mock<ISmartphoneRepository> _smartphoneRepo;
    private readonly Mock<UserManager<AppUser>> _userManager;
    private readonly PurchaseOrderController _controller;
    private readonly string _userId = "user1";

    public PurchaseOrderControllerTests()
    {
        _purchaseOrderRepo = new Mock<IPurchaseOrderRepository>();
        _smartphoneRepo = new Mock<ISmartphoneRepository>();

        var store = new Mock<IUserStore<AppUser>>();
        _userManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

        _controller = new PurchaseOrderController(_purchaseOrderRepo.Object, _smartphoneRepo.Object, _userManager.Object);

        var httpContext = new Mock<HttpContext>();
        var session = new Mock<ISession>();
        httpContext.Setup(x => x.Session).Returns(session.Object);
        session.Setup(s => s.Id).Returns("test-session-id");

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _userId)
        }, "TestAuth"));
        httpContext.Setup(x => x.User).Returns(claimsPrincipal);
        _userManager.Setup(u => u.GetUserAsync(claimsPrincipal))
            .ReturnsAsync(new AppUser { Id = _userId, UserName = "test" });

        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };
        _controller.Url = Mock.Of<Microsoft.AspNetCore.Mvc.Routing.IUrlHelper>();
    }

    [Fact]
    public async Task Index_ReturnsViewWithPagedOrders()
    {
        var orders = new List<PurchaseOrder> { new() { Id = 1, UserId = _userId } };
        var paged = new StaticPagedList<PurchaseOrder>(orders, 1, 10, 1);
        _purchaseOrderRepo.Setup(r => r.GetByUserIdPagedAsync(_userId, 1, 10)).ReturnsAsync(paged);

        var result = await _controller.Index(1, 10);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IPagedList<PurchaseOrder>>(viewResult.ViewData.Model);
        Assert.Single(model);
    }

    [Fact]
    public async Task Details_WithOwnOrder_ReturnsView()
    {
        var order = new PurchaseOrder { Id = 1, UserId = _userId };
        _purchaseOrderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var result = await _controller.Details(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PurchaseOrder>(viewResult.ViewData.Model);
    }

    [Fact]
    public async Task Details_WithWrongUser_ReturnsForbid()
    {
        var order = new PurchaseOrder { Id = 1, UserId = "other-user" };
        _purchaseOrderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var result = await _controller.Details(1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public void GetStatusClass_ReturnsCorrectClass()
    {
        Assert.Equal("warning", _controller.GetStatusClass("Pending"));
        Assert.Equal("success", _controller.GetStatusClass("Approved"));
        Assert.Equal("danger", _controller.GetStatusClass("Rejected"));
        Assert.Equal("info", _controller.GetStatusClass("Processing"));
        Assert.Equal("success", _controller.GetStatusClass("Completed"));
        Assert.Equal("secondary", _controller.GetStatusClass("Unknown"));
    }

    [Fact]
    public void GetStatusText_ReturnsCorrectText()
    {
        Assert.Equal("В ожидании", _controller.GetStatusText("Pending"));
        Assert.Equal("Одобрен", _controller.GetStatusText("Approved"));
        Assert.Equal("Отклонен", _controller.GetStatusText("Rejected"));
        Assert.Equal("В обработке", _controller.GetStatusText("Processing"));
        Assert.Equal("Завершен", _controller.GetStatusText("Completed"));
        Assert.Equal("Unknown", _controller.GetStatusText("Unknown"));
    }

    [Fact]
    public async Task Cancel_WithPendingOrder_Cancels()
    {
        var order = new PurchaseOrder { Id = 1, UserId = _userId, Status = "Pending" };
        _purchaseOrderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var result = await _controller.Cancel(1);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _purchaseOrderRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task Cancel_WithNonPending_ReturnsError()
    {
        var order = new PurchaseOrder { Id = 1, UserId = _userId, Status = "Approved" };
        _purchaseOrderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var result = await _controller.Cancel(1);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Отмена возможна только для ожидающих запросов.", _controller.TempData["Error"]);
        _purchaseOrderRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }
}
