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

public class CatalogControllerTests
{
    private readonly Mock<ISmartphoneRepository> _smartphoneRepo;
    private readonly Mock<IFavoriteRepository> _favoriteRepo;
    private readonly Mock<IReviewRepository> _reviewRepo;
    private readonly Mock<UserManager<AppUser>> _userManager;
    private readonly Mock<IComparisonRepository> _comparisonRepo;
    private readonly CatalogController _controller;
    private readonly Mock<ISession> _session;
    private readonly Mock<IServiceProvider> _serviceProvider;

    public CatalogControllerTests()
    {
        _smartphoneRepo = new Mock<ISmartphoneRepository>();
        _favoriteRepo = new Mock<IFavoriteRepository>();
        _reviewRepo = new Mock<IReviewRepository>();
        _comparisonRepo = new Mock<IComparisonRepository>();

        var store = new Mock<IUserStore<AppUser>>();
        _userManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
        _session = new Mock<ISession>();
        _serviceProvider = new Mock<IServiceProvider>();

        _controller = new CatalogController(
            _smartphoneRepo.Object, _favoriteRepo.Object, _reviewRepo.Object, _userManager.Object);

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Session).Returns(_session.Object);
        httpContext.Setup(x => x.RequestServices).Returns(_serviceProvider.Object);
        _session.Setup(s => s.Id).Returns("test-session-id");
        _serviceProvider.Setup(x => x.GetService(typeof(IComparisonRepository)))
            .Returns(_comparisonRepo.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext.Object
        };
    }

    [Fact]
    public async Task Index_ReturnsViewWithPagedSmartphones()
    {
        var phones = new List<Smartphone>
        {
            new() { Id = 1, ModelName = "Galaxy", Brand = "Samsung", Price = 100 },
            new() { Id = 2, ModelName = "iPhone", Brand = "Apple", Price = 200 }
        };
        var paged = new StaticPagedList<Smartphone>(phones, 1, 12, 2);
        _smartphoneRepo.Setup(r => r.GetFilteredPagedAsync(null, null, null, null, null, null, null, 1, 12))
            .ReturnsAsync(paged);
        _reviewRepo.Setup(r => r.GetAverageRatingAsync(It.IsAny<int>())).ReturnsAsync(4.0);
        _comparisonRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ComparisonList?)null);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IPagedList<Smartphone>>(viewResult.ViewData.Model);
        Assert.Equal(2, model.Count);
    }

    [Fact]
    public async Task Index_AppliesFilters()
    {
        var phones = new List<Smartphone> { new() { Id = 1, ModelName = "Galaxy", Brand = "Samsung", Price = 100 } };
        var paged = new StaticPagedList<Smartphone>(phones, 1, 12, 1);
        _smartphoneRepo.Setup(r => r.GetFilteredPagedAsync("Samsung", 50, 150, null, null, "price_asc", null, 1, 12))
            .ReturnsAsync(paged);
        _reviewRepo.Setup(r => r.GetAverageRatingAsync(It.IsAny<int>())).ReturnsAsync(4.0);
        _comparisonRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ComparisonList?)null);

        var result = await _controller.Index(brand: "Samsung", minPrice: 50, maxPrice: 150, sort: "price_asc");

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Samsung", viewResult.ViewData["CurrentBrand"]);
        Assert.Equal(50m, viewResult.ViewData["MinPrice"]);
        Assert.Equal(150m, viewResult.ViewData["MaxPrice"]);
    }

    [Fact]
    public async Task Index_PageCannotBeLessThanOne()
    {
        var phones = new List<Smartphone>();
        var paged = new StaticPagedList<Smartphone>(phones, 1, 12, 0);
        _smartphoneRepo.Setup(r => r.GetFilteredPagedAsync(null, null, null, null, null, null, null, 1, 12))
            .ReturnsAsync(paged);
        _reviewRepo.Setup(r => r.GetAverageRatingAsync(It.IsAny<int>())).ReturnsAsync(0);
        _comparisonRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ComparisonList?)null);

        var result = await _controller.Index(page: -5);

        Assert.IsType<ViewResult>(result);
        _smartphoneRepo.Verify(r => r.GetFilteredPagedAsync(null, null, null, null, null, null, null, 1, 12), Times.Once);
    }

    [Fact]
    public async Task Index_SetsViewBagBrands()
    {
        _smartphoneRepo.Setup(r => r.GetFilteredPagedAsync(null, null, null, null, null, null, null, 1, 12))
            .ReturnsAsync(new StaticPagedList<Smartphone>(new List<Smartphone>(), 1, 12, 0));
        _reviewRepo.Setup(r => r.GetAverageRatingAsync(It.IsAny<int>())).ReturnsAsync(0);
        _comparisonRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ComparisonList?)null);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var brands = viewResult.ViewData["Brands"] as string[];
        Assert.NotNull(brands);
        Assert.Contains("Samsung", brands);
        Assert.Contains("Apple", brands);
    }
}
