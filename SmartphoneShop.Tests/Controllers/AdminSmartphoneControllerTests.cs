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
    private readonly AppDbContext _context;
    private readonly AdminSmartphoneController _controller;

    public AdminSmartphoneControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _controller = new AdminSmartphoneController(_context);

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
        var result = await _controller.Edit(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_DeletesSmartphone()
    {
        _context.Smartphones.Add(new Smartphone { Id = 1, ModelName = "Test", Brand = "B" });
        _context.SaveChanges();

        var result = await _controller.Delete(1);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }
}
