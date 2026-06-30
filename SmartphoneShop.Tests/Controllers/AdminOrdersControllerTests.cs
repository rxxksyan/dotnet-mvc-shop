using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Enums;
using SmartphoneShop.Infrastructure.Data;
using SmartphoneShop.Web.Controllers;
using System.Security.Claims;
using Xunit;

namespace SmartphoneShop.Tests.Controllers;

public class AdminOrdersControllerTests
{
    private readonly AppDbContext _context;
    private readonly AdminOrdersController _controller;

    public AdminOrdersControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _controller = new AdminOrdersController(_context);

        var httpContext = new Mock<HttpContext>();
        var session = new Mock<ISession>();
        httpContext.Setup(x => x.Session).Returns(session.Object);
        session.Setup(s => s.Id).Returns("test-session-id");

        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };
    }

    [Fact]
    public async Task Index_ReturnsView()
    {
        var result = await _controller.Index(null, 1, 10);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Index_WithStatusFilter_FiltersOrders()
    {
        var result = await _controller.Index("Pending", 1, 10);
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Pending", viewResult.ViewData["Status"]);
    }
}
