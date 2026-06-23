using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartphoneShop.Infrastructure.Data;
using SmartphoneShop.Web.Controllers;
using System.Security.Claims;
using Xunit;

namespace SmartphoneShop.Tests.Controllers;

public class AdminPurchaseOrdersControllerTests
{
    private readonly Mock<AppDbContext> _context;
    private readonly AdminPurchaseOrdersController _controller;

    public AdminPurchaseOrdersControllerTests()
    {
        _context = new Mock<AppDbContext>();
        _controller = new AdminPurchaseOrdersController(_context.Object);

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
