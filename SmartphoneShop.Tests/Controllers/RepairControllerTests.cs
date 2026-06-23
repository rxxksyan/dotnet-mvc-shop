using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Enums;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Infrastructure.Data;
using SmartphoneShop.Web.Controllers;
using System.Security.Claims;
using Xunit;

namespace SmartphoneShop.Tests.Controllers;

public class RepairControllerTests
{
    private readonly Mock<IRepairRequestRepository> _repairRepo;
    private readonly Mock<AppDbContext> _context;
    private readonly RepairController _controller;
    private readonly string _userId = "user1";

    public RepairControllerTests()
    {
        _repairRepo = new Mock<IRepairRequestRepository>();
        _context = new Mock<AppDbContext>();

        _controller = new RepairController(_repairRepo.Object, _context.Object);

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
    public async Task Index_WhenAuthenticated_ReturnsViewWithRepairs()
    {
        var repairs = new List<RepairRequest>
        {
            new() { Id = 1, UserId = _userId, SmartphoneModel = "Galaxy" }
        };
        _repairRepo.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync(repairs);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = viewResult.ViewData["Repairs"] as IEnumerable<RepairRequest>;
        Assert.NotNull(model);
        Assert.Single(model);
    }

    [Fact]
    public async Task Create_WithValidData_CreatesRequest()
    {
        var result = await _controller.Create("Galaxy S21", "Broken screen");

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _repairRepo.Verify(r => r.AddAsync(It.Is<RepairRequest>(req =>
            req.SmartphoneModel == "Galaxy S21" &&
            req.IssueDescription == "Broken screen" &&
            req.Status == RepairStatus.New
        )), Times.Once);
    }

    [Fact]
    public async Task Create_WithEmptyFields_ReturnsError()
    {
        var result = await _controller.Create("", "");

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Заполните все поля", _controller.TempData["Error"]);
        _repairRepo.Verify(r => r.AddAsync(It.IsAny<RepairRequest>()), Times.Never);
    }
}
