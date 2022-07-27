using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Smartstore.Web.Rendering
{
    public static class IHtmlContentExtensions
    {
        public static HtmlString ToHtmlString(this IHtmlContent htmlContent)
        {
            Guard.NotNull(htmlContent, nameof(htmlContent));

            if (htmlContent is HtmlString htmlString)
            {
                return htmlString;
            }
            else
            {
                var sb = new StringBuilder();
                using var writer = new StringWriter(sb);
                htmlContent.WriteTo(writer, HtmlEncoder.Default);

                return new HtmlString(writer.ToString());
            }
        }
    }
}
