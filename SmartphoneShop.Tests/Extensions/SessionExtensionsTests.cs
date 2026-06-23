using SmartphoneShop.Web.Extensions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Text.Json;

namespace SmartphoneShop.Tests.Extensions;

public class SessionExtensionsTests
{
    [Fact]
    public void SetObject_GetObject_Roundtrip()
    {
        var data = new { Name = "test", Value = 42 };
        var session = new Mock<ISession>();
        var key = "testKey";
        byte[]? storedBytes = null;

        session.Setup(s => s.Set(key, It.IsAny<byte[]>()))
            .Callback<string, byte[]>((k, v) => storedBytes = v);
        session.Setup(s => s.TryGetValue(key, out storedBytes))
            .Returns(storedBytes != null);

        session.Object.SetObject(key, data);

        session.Setup(s => s.TryGetValue(key, out It.Ref<byte[]>.IsAny))
            .Returns((string k, out byte[] v) =>
            {
                v = storedBytes;
                return storedBytes is not null;
            });

        var result = session.Object.GetObject<object>(key);
        Assert.NotNull(result);
    }

    [Fact]
    public void GetObject_WhenNotSet_ReturnsDefault()
    {
        var session = new Mock<ISession>();
        byte[]? val = null;
        session.Setup(s => s.TryGetValue("nonexistent", out val)).Returns(false);

        var result = session.Object.GetObject<string>("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public void SetDecimal_GetDecimal_Roundtrip()
    {
        var session = new Mock<ISession>();
        var key = "price";
        byte[]? storedBytes = null;

        session.Setup(s => s.Set(key, It.IsAny<byte[]>()))
            .Callback<string, byte[]>((k, v) => storedBytes = v);
        session.Setup(s => s.TryGetValue(key, out storedBytes))
            .Returns(storedBytes != null);

        session.Object.SetDecimal(key, 99.99m);

        session.Setup(s => s.TryGetValue(key, out It.Ref<byte[]>.IsAny))
            .Returns((string k, out byte[] v) =>
            {
                v = storedBytes;
                return storedBytes is not null;
            });

        var result = session.Object.GetDecimal(key);
        Assert.Equal(99.99m, result);
    }

    [Fact]
    public void GetDecimal_WhenNotSet_ReturnsZero()
    {
        var session = new Mock<ISession>();
        byte[]? val = null;
        session.Setup(s => s.TryGetValue("nonexistent", out val)).Returns(false);

        var result = session.Object.GetDecimal("nonexistent");
        Assert.Equal(0, result);
    }
}
