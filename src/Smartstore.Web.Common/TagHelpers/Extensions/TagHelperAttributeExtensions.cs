using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers
{
    public static class TagHelperAttributeExtensions
    {
        public static string ValueAsString(this TagHelperAttribute attribute)
        {
            Guard.NotNull(attribute, nameof(attribute));

            var value = attribute.Value;

            if (value is HtmlString htmlString)
            {
                return htmlString.ToString();
            }
            else if (value is IHtmlContent htmlContent)
            {
                using var writer = new StringWriter();
                htmlContent.WriteTo(writer, HtmlEncoder.Default);

                return writer.ToString();
            }

            return value?.ToString();
        }
    }
}
