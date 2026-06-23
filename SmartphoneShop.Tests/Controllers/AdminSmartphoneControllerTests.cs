using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Infrastructure.Data;
using SmartphoneShop.Web.Controllers;
using SmartphoneShop.Web.Models;
using System.Security.Claims;
using Xunit;

namespace SmartphoneShop.Tests.Controllers;

public class AdminSmartphoneControllerTests
{
    private readonly Mock<AppDbContext> _context;
    private readonly AdminSmartphoneController _controller;

    public AdminSmartphoneControllerTests()
    {
        _context = new Mock<AppDbContext>();
        _controller = new AdminSmartphoneController(_context.Object);

        var httpContext = new Mock<HttpContext>();
        var session = new Mock<ISession>();
        httpContext.Setup(x => x.Session).Returns(session.Object);
        session.Setup(s => s.Id).Returns("test-session-id");

        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };
    }

    [Fact]
    public void Create_Get_ReturnsView()
    {
        var result = _controller.Create();
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<SmartphoneFormViewModel>(viewResult.ViewData.Model);
    }

    [Fact]
    public async Task Edit_Get_WithInvalidId_ReturnsNotFound()
    {
        _context.Setup(c => c.Smartphones.FindAsync(999))
            .ReturnsAsync((Smartphone?)null);

        var result = await _controller.Edit(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_DeletesSmartphone()
    {
        var phone = new Smartphone { Id = 1, ModelName = "Test", Brand = "B" };
        _context.Setup(c => c.Smartphones.FindAsync(1)).ReturnsAsync(phone);

        var result = await _controller.Delete(1);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }
}
