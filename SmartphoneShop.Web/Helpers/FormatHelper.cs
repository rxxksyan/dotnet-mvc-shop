using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartphoneShop.Web.Helpers
{
    public static class FormatHelper
    {
        public static IHtmlContent FormatOpinionText(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return HtmlString.Empty;

            var result = System.Text.RegularExpressions.Regex.Replace(text, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\*(.+?)\*", "<em>$1</em>");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"~~(.+?)~~", "<del>$1</del>");
            result = result.Replace("\n", "<br />");

            return new HtmlString(result);
        }
    }
}
