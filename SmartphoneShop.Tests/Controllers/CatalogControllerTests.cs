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
        httpContext.Setup(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity()));
        _session.Setup(s => s.Id).Returns("test-session-id");
        _serviceProvider.Setup(x => x.GetService(typeof(IComparisonRepository)))
            .Returns(_comparisonRepo.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext.Object
        };
    }

}
