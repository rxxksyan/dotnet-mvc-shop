using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Web.Controllers;
using System.Security.Claims;
using Xunit;

namespace SmartphoneShop.Tests.Controllers;

public class FavoritesControllerTests
{
    private readonly Mock<IFavoriteRepository> _favoriteRepo;
    private readonly Mock<ISmartphoneRepository> _smartphoneRepo;
    private readonly FavoritesController _controller;
    private readonly string _userId = "user1";

    public FavoritesControllerTests()
    {
        _favoriteRepo = new Mock<IFavoriteRepository>();
        _smartphoneRepo = new Mock<ISmartphoneRepository>();

        _controller = new FavoritesController(_favoriteRepo.Object, _smartphoneRepo.Object);

        var httpContext = new Mock<HttpContext>();
        var session = new Mock<ISession>();
        httpContext.Setup(x => x.Session).Returns(session.Object);
        session.Setup(s => s.Id).Returns("test-session-id");

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _userId)
        }, "TestAuth"));
        httpContext.Setup(x => x.User).Returns(claimsPrincipal);

        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };
    }

    [Fact]
    public async Task Index_ReturnsViewWithFavorites()
    {
        var favorites = new List<Favorite>
        {
            new() { Id = 1, UserId = _userId, SmartphoneId = 1 },
            new() { Id = 2, UserId = _userId, SmartphoneId = 2 }
        };
        _favoriteRepo.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync(favorites);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Favorite>>(viewResult.ViewData.Model);
        Assert.Equal(2, model.Count());
    }

    [Fact]
    public async Task Toggle_WithNewSmartphone_AddsFavorite()
    {
        _smartphoneRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Smartphone { Id = 1 });
        _favoriteRepo.Setup(r => r.ExistsAsync(_userId, 1)).ReturnsAsync(false);

        var result = await _controller.Toggle(1);

        Assert.IsType<OkResult>(result);
        _favoriteRepo.Verify(r => r.AddAsync(It.Is<Favorite>(f => f.SmartphoneId == 1)), Times.Once);
    }

    [Fact]
    public async Task Toggle_WithExistingFavorite_RemovesFavorite()
    {
        _smartphoneRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Smartphone { Id = 1 });
        _favoriteRepo.Setup(r => r.ExistsAsync(_userId, 1)).ReturnsAsync(true);
        _favoriteRepo.Setup(r => r.GetByUserAndSmartphoneAsync(_userId, 1))
            .ReturnsAsync(new Favorite { Id = 5, UserId = _userId, SmartphoneId = 1 });

        var result = await _controller.Toggle(1);

        Assert.IsType<OkResult>(result);
        _favoriteRepo.Verify(r => r.DeleteAsync(5), Times.Once);
    }

    [Fact]
    public async Task Toggle_WithNonExistentSmartphone_ReturnsNotFound()
    {
        _smartphoneRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Smartphone?)null);

        var result = await _controller.Toggle(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Remove_DeletesFavorite()
    {
        var result = await _controller.Remove(1);

        Assert.IsType<RedirectToActionResult>(result);
        _favoriteRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }
}
