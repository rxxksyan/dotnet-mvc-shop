using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Web.Controllers;
using SmartphoneShop.Web.Models;
using System.Security.Claims;
using Xunit;

namespace SmartphoneShop.Tests.Controllers;

public class HomeControllerTests
{
    private readonly Mock<ISmartphoneRepository> _smartphoneRepo;
    private readonly Mock<IFavoriteRepository> _favoriteRepo;
    private readonly Mock<IReviewRepository> _reviewRepo;
    private readonly Mock<IComparisonRepository> _comparisonRepo;
    private readonly HomeController _controller;
    private readonly Mock<ISession> _session;
    private readonly Mock<IServiceProvider> _serviceProvider;

    public HomeControllerTests()
    {
        _smartphoneRepo = new Mock<ISmartphoneRepository>();
        _favoriteRepo = new Mock<IFavoriteRepository>();
        _reviewRepo = new Mock<IReviewRepository>();
        _comparisonRepo = new Mock<IComparisonRepository>();
        _session = new Mock<ISession>();
        _serviceProvider = new Mock<IServiceProvider>();

        _controller = new HomeController(_smartphoneRepo.Object, _favoriteRepo.Object, _reviewRepo.Object);

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Session).Returns(_session.Object);
        httpContext.Setup(x => x.RequestServices).Returns(_serviceProvider.Object);
        httpContext.Setup(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity()));
        _serviceProvider.Setup(x => x.GetService(typeof(IComparisonRepository)))
            .Returns(_comparisonRepo.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext.Object
        };

        var tempDataFactory = new Mock<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory>();
        var tempDataProvider = new Mock<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>();
        tempDataFactory.Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
            .Returns(new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                httpContext.Object, tempDataProvider.Object));
        _serviceProvider.Setup(s => s.GetService(typeof(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory)))
            .Returns(tempDataFactory.Object);
    }

    [Fact]
    public async Task Index_ReturnsViewWithFeaturedSmartphones()
    {
        var phones = new List<Smartphone>
        {
            new() { Id = 1, ModelName = "Phone1", Brand = "Brand1", Price = 100, IsFeatured = true },
            new() { Id = 2, ModelName = "Phone2", Brand = "Brand2", Price = 200, IsFeatured = true }
        };
        _smartphoneRepo.Setup(r => r.GetFeaturedAsync()).ReturnsAsync(phones);
        _reviewRepo.Setup(r => r.GetAverageRatingAsync(It.IsAny<int>())).ReturnsAsync(4.5);
        _comparisonRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ComparisonList?)null);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Smartphone>>(viewResult.ViewData.Model);
        Assert.Equal(2, model.Count);
    }

    [Fact]
    public void Privacy_ReturnsView()
    {
        var result = _controller.Privacy();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Error_ReturnsViewWithModel()
    {
        var result = _controller.Error(null);
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ErrorViewModel>(viewResult.ViewData.Model);
    }

    [Fact]
    public async Task Index_SetsFavoriteIds_WhenAuthenticated()
    {
        var userId = "user1";
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "testuser")
        }, "TestAuth"));

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Session).Returns(_session.Object);
        httpContext.Setup(x => x.RequestServices).Returns(_serviceProvider.Object);
        httpContext.Setup(x => x.User).Returns(claimsPrincipal);
        _serviceProvider.Setup(x => x.GetService(typeof(IComparisonRepository)))
            .Returns(_comparisonRepo.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext.Object
        };

        var phones = new List<Smartphone> { new() { Id = 1, ModelName = "Phone1", Brand = "B", Price = 100, IsFeatured = true } };
        _smartphoneRepo.Setup(r => r.GetFeaturedAsync()).ReturnsAsync(phones);
        _reviewRepo.Setup(r => r.GetAverageRatingAsync(It.IsAny<int>())).ReturnsAsync(4.0);
        _favoriteRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Favorite>
        {
            new() { UserId = userId, SmartphoneId = 1 }
        });
        _comparisonRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((ComparisonList?)null);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var favoriteIds = viewResult.ViewData["FavoriteIds"] as HashSet<int>;
        Assert.NotNull(favoriteIds);
        Assert.Contains(1, favoriteIds);
    }
}
