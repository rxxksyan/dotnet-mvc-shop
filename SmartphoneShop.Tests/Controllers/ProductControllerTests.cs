using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Web.Controllers;
using SmartphoneShop.Web.Extensions;
using System.Security.Claims;
using System.Text;
using X.PagedList;
using Xunit;

namespace SmartphoneShop.Tests.Controllers;

public class ProductControllerTests
{
    private readonly Mock<ISmartphoneRepository> _smartphoneRepo;
    private readonly Mock<IReviewRepository> _reviewRepo;
    private readonly Mock<IFavoriteRepository> _favoriteRepo;
    private readonly Mock<UserManager<AppUser>> _userManager;
    private readonly Mock<ILogger<ProductController>> _logger;
    private readonly ProductController _controller;
    private readonly Mock<ISession> _session;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;

    public ProductControllerTests()
    {
        _smartphoneRepo = new Mock<ISmartphoneRepository>();
        _reviewRepo = new Mock<IReviewRepository>();
        _favoriteRepo = new Mock<IFavoriteRepository>();
        _logger = new Mock<ILogger<ProductController>>();

        var store = new Mock<IUserStore<AppUser>>();
        _userManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
        _session = new Mock<ISession>();
        _httpContextAccessor = new Mock<IHttpContextAccessor>();

        _controller = new ProductController(
            _smartphoneRepo.Object, _reviewRepo.Object, _favoriteRepo.Object,
            Mock.Of<IOrderRepository>(), Mock.Of<IPurchaseOrderRepository>(),
            _userManager.Object, _logger.Object);

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Session).Returns(_session.Object);
        httpContext.Setup(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity()));
        _session.Setup(s => s.Id).Returns("test-session-id");

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext.Object
        };

        var tempDataProvider = Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>();
        _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(), tempDataProvider);
    }

    [Fact]
    public async Task Details_WithValidId_ReturnsViewWithSmartphone()
    {
        var phone = new Smartphone { Id = 1, ModelName = "Galaxy", Brand = "Samsung", Price = 100 };
        _smartphoneRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phone);
        _reviewRepo.Setup(r => r.GetBySmartphoneIdPagedAsync(1, 1, 5))
            .ReturnsAsync(new StaticPagedList<Review>(new List<Review>(), 1, 5, 0));
        _reviewRepo.Setup(r => r.GetAverageRatingAsync(1)).ReturnsAsync(4.5);

        var result = await _controller.Details(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Smartphone>(viewResult.ViewData.Model);
        Assert.Equal("Galaxy", model.ModelName);
    }

    [Fact]
    public async Task Details_WithInvalidId_ReturnsNotFound()
    {
        _smartphoneRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Smartphone?)null);

        var result = await _controller.Details(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddReview_WithoutComment_ReturnsError()
    {
        var userId = "user1";
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "TestAuth"));

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Session).Returns(_session.Object);
        httpContext.Setup(x => x.User).Returns(claimsPrincipal);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };

        var result = await _controller.AddReview(1, 5, "");

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        _reviewRepo.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task AddReview_DuplicateReview_ReturnsError()
    {
        var userId = "user1";
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "TestAuth"));

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Session).Returns(_session.Object);
        httpContext.Setup(x => x.User).Returns(claimsPrincipal);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };

        _reviewRepo.Setup(r => r.UserHasReviewAsync(userId, 1)).ReturnsAsync(true);

        var result = await _controller.AddReview(1, 5, "Great!");

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        _reviewRepo.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task UpdateReview_ByOwner_UpdatesReview()
    {
        var userId = "user1";
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "TestAuth"));

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Session).Returns(_session.Object);
        httpContext.Setup(x => x.User).Returns(claimsPrincipal);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };

        var existingReview = new Review { Id = 1, UserId = userId, SmartphoneId = 1, Comment = "Old", Rating = 3 };
        _reviewRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingReview);

        var result = await _controller.UpdateReview(1, 5, "Updated comment");

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal(5, existingReview.Rating);
        Assert.Equal("Updated comment", existingReview.Comment);
        _reviewRepo.Verify(r => r.UpdateAsync(existingReview), Times.Once);
    }

    [Fact]
    public async Task UpdateReview_ByWrongUser_ReturnsNotFound()
    {
        var userId = "user1";
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "TestAuth"));

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Session).Returns(_session.Object);
        httpContext.Setup(x => x.User).Returns(claimsPrincipal);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };

        var existingReview = new Review { Id = 1, UserId = "other-user", Comment = "Old", Rating = 3 };
        _reviewRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingReview);

        var result = await _controller.UpdateReview(1, 5, "Updated");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task BuyNow_SetsSessionValues()
    {
        var userId = "user1";
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "TestAuth"));

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Session).Returns(_session.Object);
        httpContext.Setup(x => x.User).Returns(claimsPrincipal);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };

        var phone = new Smartphone { Id = 1, ModelName = "Galaxy", Brand = "Samsung", Price = 100 };
        _smartphoneRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phone);

        var result = await _controller.BuyNow(1);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Checkout", redirectResult.ActionName);
        Assert.Equal("Order", redirectResult.ControllerName);

        _session.Verify(s => s.Set("BuyNowSmartphoneId", It.IsAny<byte[]>()), Times.Once);
        _session.Verify(s => s.Set("BuyNowSmartphoneName", It.IsAny<byte[]>()), Times.Once);
    }

    [Fact]
    public async Task BuyNow_WithInvalidId_ReturnsNotFound()
    {
        var userId = "user1";
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "TestAuth"));

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Session).Returns(_session.Object);
        httpContext.Setup(x => x.User).Returns(claimsPrincipal);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };

        _smartphoneRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Smartphone?)null);

        var result = await _controller.BuyNow(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_WhenAuthenticated_SetsFavoriteAndReviewFlags()
    {
        var userId = "user1";
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "TestAuth"));

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Session).Returns(_session.Object);
        httpContext.Setup(x => x.User).Returns(claimsPrincipal);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };

        var phone = new Smartphone { Id = 1, ModelName = "Galaxy", Brand = "Samsung", Price = 100 };
        _smartphoneRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phone);
        _reviewRepo.Setup(r => r.GetBySmartphoneIdPagedAsync(1, 1, 5))
            .ReturnsAsync(new StaticPagedList<Review>(new List<Review>(), 1, 5, 0));
        _reviewRepo.Setup(r => r.GetAverageRatingAsync(1)).ReturnsAsync(4.5);
        _favoriteRepo.Setup(r => r.ExistsAsync(userId, 1)).ReturnsAsync(true);
        _reviewRepo.Setup(r => r.UserHasReviewAsync(userId, 1)).ReturnsAsync(true);
        _reviewRepo.Setup(r => r.GetBySmartphoneIdAsync(1)).ReturnsAsync(new List<Review>
        {
            new() { Id = 10, UserId = userId, Comment = "Nice", Rating = 4 }
        });

        var user = new AppUser { Id = userId, UserName = "testuser" };
        _userManager.Setup(u => u.GetUserAsync(claimsPrincipal)).ReturnsAsync(user);

        var result = await _controller.Details(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.True((bool)viewResult.ViewData["IsFavorite"]!);
        Assert.True((bool)viewResult.ViewData["UserHasReview"]!);
        Assert.Equal(10, viewResult.ViewData["UserReviewId"]);
    }
}
