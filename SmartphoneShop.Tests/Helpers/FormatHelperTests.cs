using SmartphoneShop.Web.Helpers;
using Xunit;

namespace SmartphoneShop.Tests.Helpers;

public class FormatHelperTests
{
    [Fact]
    public void FormatOpinionText_Null_ReturnsEmpty()
    {
        var result = FormatHelper.FormatOpinionText(null);
        Assert.Empty(result.ToString());
    }

    [Fact]
    public void FormatOpinionText_Empty_ReturnsEmpty()
    {
        var result = FormatHelper.FormatOpinionText("");
        Assert.Empty(result.ToString());
    }

    [Fact]
    public void FormatOpinionText_BoldMarkdown_ConvertsToStrong()
    {
        var result = FormatHelper.FormatOpinionText("This is **bold** text").ToString();
        Assert.Contains("<strong>bold</strong>", result);
    }

    [Fact]
    public void FormatOpinionText_ItalicMarkdown_ConvertsToEm()
    {
        var result = FormatHelper.FormatOpinionText("This is *italic* text").ToString();
        Assert.Contains("<em>italic</em>", result);
    }

    [Fact]
    public void FormatOpinionText_StrikethroughMarkdown_ConvertsToDel()
    {
        var result = FormatHelper.FormatOpinionText("This is ~~strikethrough~~ text").ToString();
        Assert.Contains("<del>strikethrough</del>", result);
    }

    [Fact]
    public void FormatOpinionText_Newlines_ConvertsToBr()
    {
        var result = FormatHelper.FormatOpinionText("Line1\nLine2").ToString();
        Assert.Contains("<br />", result);
    }

    [Fact]
    public void FormatOpinionText_CombinedMarkdown()
    {
        var result = FormatHelper.FormatOpinionText("**Bold** and *italic* and ~~strike~~").ToString();
        Assert.Contains("<strong>Bold</strong>", result);
        Assert.Contains("<em>italic</em>", result);
        Assert.Contains("<del>strike</del>", result);
    }
}
