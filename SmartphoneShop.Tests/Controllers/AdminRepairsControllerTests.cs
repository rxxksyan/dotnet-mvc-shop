using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Enums;
using SmartphoneShop.Infrastructure.Data;
using SmartphoneShop.Web.Controllers;
using System.Security.Claims;
using Xunit;

namespace SmartphoneShop.Tests.Controllers;

public class AdminRepairsControllerTests
{
    private readonly Mock<AppDbContext> _context;
    private readonly Mock<UserManager<AppUser>> _userManager;
    private readonly AdminRepairsController _controller;

    public AdminRepairsControllerTests()
    {
        var store = new Mock<IUserStore<AppUser>>();
        _userManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
        _context = new Mock<AppDbContext>();

        _controller = new AdminRepairsController(_context.Object, _userManager.Object);

        var httpContext = new Mock<HttpContext>();
        var session = new Mock<ISession>();
        httpContext.Setup(x => x.Session).Returns(session.Object);
        session.Setup(s => s.Id).Returns("test-session-id");

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin1")
        }, "TestAuth"));
        httpContext.Setup(x => x.User).Returns(claimsPrincipal);

        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };
    }

    [Fact]
    public async Task Index_ReturnsView()
    {
        var result = await _controller.Index(null, null, 1, 10);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Index_WithStatusFilter_FiltersRepairs()
    {
        var result = await _controller.Index("New", null, 1, 10);
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("New", viewResult.ViewData["Status"]);
    }

    [Fact]
    public async Task Details_WithInvalidId_ReturnsNotFound()
    {
        _context.Setup(c => c.RepairRequests.FindAsync(999))
            .ReturnsAsync((RepairRequest?)null);

        var result = await _controller.Details(999);
        Assert.IsType<NotFoundResult>(result);
    }
}
