using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Infrastructure.Data;
using SmartphoneShop.Web.Controllers;
using System.Security.Claims;
using Xunit;

namespace SmartphoneShop.Tests.Controllers;

public class AdminControllerTests
{
    private readonly Mock<UserManager<AppUser>> _userManager;
    private readonly Mock<RoleManager<IdentityRole>> _roleManager;
    private readonly AppDbContext _context;
    private readonly AdminController _controller;
    private readonly string _userId = "admin1";

    public AdminControllerTests()
    {
        var store = new Mock<IUserStore<AppUser>>();
        _userManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        _roleManager = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null, null, null, null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _controller = new AdminController(_userManager.Object, _roleManager.Object, _context);

        var httpContext = new Mock<HttpContext>();
        var session = new Mock<ISession>();
        httpContext.Setup(x => x.Session).Returns(session.Object);
        session.Setup(s => s.Id).Returns("test-session-id");

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _userId)
        }, "TestAuth"));
        httpContext.Setup(x => x.User).Returns(claimsPrincipal);
        _userManager.Setup(u => u.GetUserId(claimsPrincipal)).Returns(_userId);

        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };

        var tempDataProvider = Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>();
        _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(), tempDataProvider);
    }

    [Fact]
    public async Task Users_ReturnsView()
    {
        var users = new List<AppUser> { new() { Id = "1", UserName = "test" } }.AsQueryable();
        _userManager.Setup(u => u.Users).Returns(users);

        var result = await _controller.Users(null, 1, 20);

        var viewResult = Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task DeleteUser_WithSelf_ReturnsError()
    {
        var user = new AppUser { Id = _userId, UserName = "test" };
        _userManager.Setup(u => u.FindByIdAsync(_userId)).ReturnsAsync(user);
        _userManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(_userId);

        var result = await _controller.DeleteUser(_userId);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Users", redirectResult.ActionName);
        Assert.Equal("Нельзя удалить самого себя", _controller.TempData["Error"]);
    }
}
