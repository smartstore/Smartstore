using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Common;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Rendering
{
    public static class IHtmlContentExtensions
    {
        public static HtmlString ToHtmlString(this IHtmlContent content)
        {
            Guard.NotNull(content, nameof(content));

            if (content is HtmlString htmlString)
            {
                return htmlString;
            }
            else
            {
                var sb = new StringBuilder(100);
                using var writer = new StringWriter(sb);
                content.WriteTo(writer, HtmlEncoder.Default);

                return new HtmlString(writer.ToString());
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current <see cref="IHtmlContent"/> has actual content (that it is not empty or not all whitespace).
        /// </summary>
        public static bool HasContent(this IHtmlContent content)
        {
            Guard.NotNull(content, nameof(content));

            switch (content)
            {
                case TagBuilder:
                case Money:
                case LocalizedHtmlString:
                case TagHelperAttribute:
                    return true;
                case HtmlString htmlString:
                    return htmlString.Value.HasValue();
                case LocalizedString locString:
                    return locString.Value.HasValue();
                case TagHelperContent tagHelperContent:
                    return !tagHelperContent.IsEmptyOrWhiteSpace;
                case HtmlContentBuilder builder:
                    if (builder.Count == 0)
                    {
                        return false;
                    }
                    break;
                case TagHelperOutput output:
                    var hasValue = output.TagName.HasValue()
                        || !output.Content.IsEmptyOrWhiteSpace
                        || !output.PreElement.IsEmptyOrWhiteSpace
                        || !output.PreContent.IsEmptyOrWhiteSpace
                        || !output.PostContent.IsEmptyOrWhiteSpace
                        || !output.PostElement.IsEmptyOrWhiteSpace;

                    return hasValue;
            }

            using var writer = new ThrowOnValueWriter();
            try
            {
                // Use NullHtmlEncoder to avoid treating encoded whitespace as non-whitespace e.g. "\t" as "&#x9;".
                content.WriteTo(writer, NullHtmlEncoder.Default);

                // If ThrowOnValueWriter did not throw, no non-whitespace content was encountered.
                return false;
            }
            catch
            {
                // ThrowOnValueWriter throws on purpose if it encounters the first non-whitespace content.
                return true;
            }
        }

        /// <summary>
        /// Overrides Write() to throw if the content written is non empty/whitespace.
        /// </summary>
        class ThrowOnValueWriter : TextWriter
        {
            public override Encoding Encoding
            {
                get => Encoding.UTF8;
            }

            public override void Write(char value)
            {
                if (!char.IsWhiteSpace(value))
                {
                    throw new SmartException();
                }
            }

            public override void Write(string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    throw new SmartException();
                }
            }
        }
    }
}
