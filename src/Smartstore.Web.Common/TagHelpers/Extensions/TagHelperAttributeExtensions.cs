using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers
{
    public static class TagHelperAttributeExtensions
    {
        public static string ValueAsString(this TagHelperAttribute attribute)
        {
            Guard.NotNull(attribute);

            var value = attribute.Value;

            if (value is IHtmlContent htmlContent)
            {
                return htmlContent.ToHtmlString().ToString();
            }

            return value?.ToString();
        }
    }
}
