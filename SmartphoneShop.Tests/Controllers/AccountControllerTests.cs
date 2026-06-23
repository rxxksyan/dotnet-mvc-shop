using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Web.Controllers;
using System.Security.Claims;
using Xunit;

namespace SmartphoneShop.Tests.Controllers;

public class AccountControllerTests
{
    private readonly Mock<UserManager<AppUser>> _userManager;
    private readonly Mock<SignInManager<AppUser>> _signInManager;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        var store = new Mock<IUserStore<AppUser>>();
        _userManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
        _signInManager = new Mock<SignInManager<AppUser>>(
            _userManager.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);

        _controller = new AccountController(_userManager.Object, _signInManager.Object);

        var httpContext = new Mock<HttpContext>();
        var session = new Mock<ISession>();
        httpContext.Setup(x => x.Session).Returns(session.Object);
        session.Setup(s => s.Id).Returns("test-session-id");
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };
    }

    [Fact]
    public void Login_Get_ReturnsView()
    {
        var result = _controller.Login(null);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Login_Post_WithValidCredentials_RedirectsToHome()
    {
        var user = new AppUser { Id = "1", Email = "test@test.com", UserName = "test@test.com" };
        _userManager.Setup(u => u.FindByEmailAsync("test@test.com")).ReturnsAsync(user);
        _signInManager.Setup(s => s.PasswordSignInAsync(user, "password", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var result = await _controller.Login("test@test.com", "password");

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task Login_Post_WithInvalidEmail_ReturnsViewWithError()
    {
        _userManager.Setup(u => u.FindByEmailAsync("wrong@test.com")).ReturnsAsync((AppUser?)null);

        var result = await _controller.Login("wrong@test.com", "password");

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Неверный email или пароль", _controller.TempData["Error"]);
    }

    [Fact]
    public async Task Login_Post_WithWrongPassword_ReturnsViewWithError()
    {
        var user = new AppUser { Id = "1", Email = "test@test.com" };
        _userManager.Setup(u => u.FindByEmailAsync("test@test.com")).ReturnsAsync(user);
        _signInManager.Setup(s => s.PasswordSignInAsync(user, "wrong", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var result = await _controller.Login("test@test.com", "wrong");

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Неверный email или пароль", _controller.TempData["Error"]);
    }

    [Fact]
    public async Task Login_Post_WithEmptyFields_ReturnsViewWithError()
    {
        var result = await _controller.Login("", "");

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Заполните все поля", _controller.TempData["Error"]);
    }

    [Fact]
    public void Register_Get_ReturnsView()
    {
        var result = _controller.Register();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Register_Post_WithValidData_CreatesUser()
    {
        _userManager.Setup(u => u.FindByEmailAsync("new@test.com")).ReturnsAsync((AppUser?)null);
        _userManager.Setup(u => u.CreateAsync(It.IsAny<AppUser>(), "password123"))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(u => u.AddToRoleAsync(It.IsAny<AppUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);
        _signInManager.Setup(s => s.SignInAsync(It.IsAny<AppUser>(), false, null))
            .Returns(Task.CompletedTask);

        var result = await _controller.Register("John Doe", "new@test.com", "1234567890", "password123", "password123");

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
        _userManager.Verify(u => u.CreateAsync(It.Is<AppUser>(a => a.FullName == "John Doe"), "password123"), Times.Once);
    }

    [Fact]
    public async Task Register_Post_WithExistingEmail_ReturnsError()
    {
        _userManager.Setup(u => u.FindByEmailAsync("existing@test.com"))
            .ReturnsAsync(new AppUser { Email = "existing@test.com" });

        var result = await _controller.Register("John", "existing@test.com", "123", "password123", "password123");

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Пользователь с таким email уже существует", _controller.TempData["Error"]);
    }

    [Fact]
    public async Task Register_Post_PasswordMismatch_ReturnsError()
    {
        var result = await _controller.Register("John", "test@test.com", "123", "password123", "different");

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Пароли не совпадают", _controller.TempData["Error"]);
    }

    [Fact]
    public async Task Register_Post_PasswordTooShort_ReturnsError()
    {
        var result = await _controller.Register("John", "test@test.com", "123", "ab", "ab");

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Пароль должен быть не менее 6 символов", _controller.TempData["Error"]);
    }

    [Fact]
    public async Task Register_Post_EmptyFields_ReturnsError()
    {
        var result = await _controller.Register("", "", "", "", "");

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Заполните все поля", _controller.TempData["Error"]);
    }

    [Fact]
    public async Task Logout_SignsOutAndRedirects()
    {
        var result = await _controller.Logout();

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
        _signInManager.Verify(s => s.SignOutAsync(), Times.Once);
    }

    [Fact]
    public void AccessDenied_ReturnsView()
    {
        var result = _controller.AccessDenied();
        Assert.IsType<ViewResult>(result);
    }
}
