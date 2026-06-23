using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartphoneShop.Infrastructure.Data;
using SmartphoneShop.Web.Controllers;
using SmartphoneShop.Web.Services;
using System.Security.Claims;
using Xunit;

namespace SmartphoneShop.Tests.Controllers;

public class AdminReportsControllerTests
{
    private readonly Mock<AppDbContext> _context;
    private readonly Mock<ReportGenerator> _reportGenerator;
    private readonly AdminReportsController _controller;

    public AdminReportsControllerTests()
    {
        _context = new Mock<AppDbContext>();
        _reportGenerator = new Mock<ReportGenerator>();
        _controller = new AdminReportsController(_context.Object, _reportGenerator.Object);

        var httpContext = new Mock<HttpContext>();
        var session = new Mock<ISession>();
        httpContext.Setup(x => x.Session).Returns(session.Object);
        session.Setup(s => s.Id).Returns("test-session-id");

        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };
    }

    [Fact]
    public void Index_ReturnsView()
    {
        var result = _controller.Index();
        Assert.IsType<ViewResult>(result);
    }
}
