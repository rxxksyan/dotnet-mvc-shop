using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Infrastructure.Data;
using SmartphoneShop.Web.Controllers;
using System.Security.Claims;
using Xunit;

namespace SmartphoneShop.Tests.Controllers;

public class ExpertOpinionControllerTests
{
    private readonly Mock<AppDbContext> _context;
    private readonly Mock<UserManager<AppUser>> _userManager;
    private readonly ExpertOpinionController _controller;
    private readonly string _userId = "expert1";

    public ExpertOpinionControllerTests()
    {
        var store = new Mock<IUserStore<AppUser>>();
        _userManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
        _context = new Mock<AppDbContext>();

        _controller = new ExpertOpinionController(_context.Object, _userManager.Object);

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
            .ReturnsAsync(new AppUser { Id = _userId, UserName = "expert" });

        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };
    }

    [Fact]
    public async Task Create_WithoutText_Redirects()
    {
        var result = await _controller.Create(1, 2, "", null);
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Comparison", redirectResult.ControllerName);
    }

    [Fact]
    public async Task Create_WithoutExpertRole_Redirects()
    {
        _userManager.Setup(u => u.GetRolesAsync(It.IsAny<AppUser>()))
            .ReturnsAsync(new List<string> { "User" });

        var result = await _controller.Create(1, 2, "Some text", null);
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Comparison", redirectResult.ControllerName);
    }

    [Fact]
    public async Task Edit_Get_WithInvalidId_ReturnsNotFound()
    {
        _context.Setup(c => c.ExpertOpinions.FindAsync(999))
            .ReturnsAsync((ExpertOpinion?)null);

        var result = await _controller.Edit(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        _context.Setup(c => c.ExpertOpinions.FindAsync(999))
            .ReturnsAsync((ExpertOpinion?)null);

        var result = await _controller.Delete(999);
        Assert.IsType<NotFoundResult>(result);
    }
}
