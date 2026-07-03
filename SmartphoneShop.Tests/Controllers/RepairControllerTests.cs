using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    private readonly AppDbContext _context;
    private readonly RepairController _controller;
    private readonly string _userId = "user1";

    public RepairControllerTests()
    {
        _repairRepo = new Mock<IRepairRequestRepository>();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _controller = new RepairController(_repairRepo.Object, _context);

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

        var tempDataProvider = Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>();
        _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(), tempDataProvider);
    }

    [Fact]
    public async Task Create_WithEmptyFields_ReturnsError()
    {
        var result = await _controller.Create("", "", "", false);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Заполните все поля", _controller.TempData["Error"]);
        _repairRepo.Verify(r => r.AddAsync(It.IsAny<RepairRequest>()), Times.Never);
    }
}
