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

public class AdminControllerTests
{
    private readonly Mock<UserManager<AppUser>> _userManager;
    private readonly Mock<RoleManager<IdentityRole>> _roleManager;
    private readonly Mock<AppDbContext> _context;
    private readonly AdminController _controller;
    private readonly string _userId = "admin1";

    public AdminControllerTests()
    {
        var store = new Mock<IUserStore<AppUser>>();
        _userManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        _roleManager = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null, null, null, null);
        _context = new Mock<AppDbContext>();

        _controller = new AdminController(_userManager.Object, _roleManager.Object, _context.Object);

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
    }

    [Fact]
    public async Task Index_ReturnsViewWithCounts()
    {
        var usersMock = new Mock<IQueryable<AppUser>>();
        _userManager.Setup(u => u.Users).Returns(usersMock.Object);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
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
    public async Task AssignRole_CreatesRoleIfNotExists()
    {
        var user = new AppUser { Id = "1", UserName = "test" };
        _userManager.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(user);
        _roleManager.Setup(r => r.RoleExistsAsync("Expert")).ReturnsAsync(false);
        _roleManager.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        _userManager.Setup(u => u.AddToRoleAsync(user, "Expert"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _controller.AssignRole("1", "Expert");

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Users", redirectResult.ActionName);
        _roleManager.Verify(r => r.CreateAsync(It.IsAny<IdentityRole>()), Times.Once);
        _userManager.Verify(u => u.AddToRoleAsync(user, "Expert"), Times.Once);
    }

    [Fact]
    public async Task RemoveRole_WithBaseRole_ReturnsError()
    {
        var user = new AppUser { Id = "1", UserName = "test" };
        _userManager.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(user);

        var result = await _controller.RemoveRole("1", "User");

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Users", redirectResult.ActionName);
        Assert.Equal("Нельзя удалить базовую роль Пользователь", _controller.TempData["Error"]);
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
