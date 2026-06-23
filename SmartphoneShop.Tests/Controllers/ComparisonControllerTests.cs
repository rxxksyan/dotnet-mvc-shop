using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Infrastructure.Data;
using SmartphoneShop.Web.Controllers;
using System.Security.Claims;
using Xunit;

namespace SmartphoneShop.Tests.Controllers;

public class ComparisonControllerTests
{
    private readonly Mock<IComparisonRepository> _comparisonRepo;
    private readonly Mock<ISmartphoneRepository> _smartphoneRepo;
    private readonly Mock<ILogger<ComparisonController>> _logger;
    private readonly Mock<AppDbContext> _context;
    private readonly Mock<UserManager<AppUser>> _userManager;
    private readonly ComparisonController _controller;
    private readonly Mock<ISession> _session;
    private readonly string _userId = "user1";

    public ComparisonControllerTests()
    {
        _comparisonRepo = new Mock<IComparisonRepository>();
        _smartphoneRepo = new Mock<ISmartphoneRepository>();
        _logger = new Mock<ILogger<ComparisonController>>();
        _context = new Mock<AppDbContext>();
        var store = new Mock<IUserStore<AppUser>>();
        _userManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
        _session = new Mock<ISession>();

        _controller = new ComparisonController(
            _comparisonRepo.Object, _smartphoneRepo.Object, _logger.Object, _context.Object, _userManager.Object);

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Session).Returns(_session.Object);
        _session.Setup(s => s.Id).Returns("test-session-id");

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _userId)
        }, "TestAuth"));
        httpContext.Setup(x => x.User).Returns(claimsPrincipal);

        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };
    }

    private ComparisonList CreateTestList()
    {
        return new ComparisonList
        {
            Id = 1,
            SessionId = "test-session-id",
            Items = new List<ComparisonItem>
            {
                new() { Id = 1, ComparisonListId = 1, SmartphoneId = 1, Smartphone = new Smartphone { Id = 1, ModelName = "Phone1" } },
                new() { Id = 2, ComparisonListId = 1, SmartphoneId = 2, Smartphone = new Smartphone { Id = 2, ModelName = "Phone2" } }
            }
        };
    }

    [Fact]
    public async Task Index_ReturnsViewWithComparisonList()
    {
        var list = CreateTestList();
        _comparisonRepo.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync(list);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ComparisonList>(viewResult.ViewData.Model);
        Assert.Equal(1, model.Id);
    }

    [Fact]
    public async Task Count_ReturnsJsonWithCount()
    {
        var list = CreateTestList();
        _comparisonRepo.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync(list);

        var result = await _controller.Count();

        var jsonResult = Assert.IsType<JsonResult>(result);
        var value = jsonResult.Value;
        var count = value?.GetType().GetProperty("count")?.GetValue(value);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Add_WithValidSmartphone_AddsToComparison()
    {
        var list = CreateTestList();
        list.Items.Clear();
        _comparisonRepo.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync(list);
        _smartphoneRepo.Setup(r => r.GetByIdAsync(3))
            .ReturnsAsync(new Smartphone { Id = 3, ModelName = "Phone3" });

        var result = await _controller.Add(3);

        Assert.IsType<RedirectToActionResult>(result);
        _comparisonRepo.Verify(r => r.AddItemAsync(It.IsAny<ComparisonItem>()), Times.Once);
    }

    [Fact]
    public async Task Add_WithNonExistentSmartphone_ReturnsNotFound()
    {
        _smartphoneRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Smartphone?)null);

        var result = await _controller.Add(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Add_WhenListFull_ReturnsLimitMessage()
    {
        var list = CreateTestList();
        list.Items = new List<ComparisonItem>
        {
            new() { SmartphoneId = 1 }, new() { SmartphoneId = 2 },
            new() { SmartphoneId = 3 }, new() { SmartphoneId = 4 }
        };
        _comparisonRepo.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync(list);
        _smartphoneRepo.Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(new Smartphone { Id = 5 });

        var result = await _controller.Add(5);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal("Product", redirectResult.ControllerName);
    }

    [Fact]
    public async Task Remove_RemovesItem()
    {
        var result = await _controller.Remove(1);

        Assert.IsType<RedirectToActionResult>(result);
        _comparisonRepo.Verify(r => r.RemoveItemAsync(1), Times.Once);
    }

    [Fact]
    public async Task Clear_ClearsList()
    {
        var list = CreateTestList();
        _comparisonRepo.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync(list);

        var result = await _controller.Clear();

        Assert.IsType<RedirectToActionResult>(result);
        _comparisonRepo.Verify(r => r.ClearAsync(list.Id), Times.Once);
    }

    [Fact]
    public async Task RemoveBySmartphoneId_RemovesItem()
    {
        var list = CreateTestList();
        _comparisonRepo.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync(list);

        var result = await _controller.RemoveBySmartphoneId(1);

        var jsonResult = Assert.IsType<OkObjectResult>(result);
        _comparisonRepo.Verify(r => r.RemoveItemBySmartphoneIdAsync(list.Id, 1), Times.Once);
    }
}
