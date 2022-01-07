using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Smartstore.Utilities;

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
                using var psb = StringBuilderPool.Instance.Get(out var sb);
                using var writer = new StringWriter(sb);
                htmlContent.WriteTo(writer, HtmlEncoder.Default);

                return new HtmlString(writer.ToString());
            }
        }
    }
}
