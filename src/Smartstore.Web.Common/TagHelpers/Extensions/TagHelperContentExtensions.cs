using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers
{
    public static class TagHelperContentExtensions
    {
        /// <summary>
        /// Prepends <paramref name="unencoded"/> to the existing content.
        /// </summary>
        /// <param name="unencoded">The <see cref="string"/> to be prepended.</param>
        /// <returns>A reference to this instance after the prepend operation has completed.</returns>
        public static TagHelperContent Prepend(this TagHelperContent content, string unencoded)
        {
            if (content.IsEmptyOrWhiteSpace)
            {
                return content.SetContent(unencoded);
            }
            else
            {
                var builder = new HtmlContentBuilder(2);
                // Add new content first
                builder.Append(unencoded);
                // Then append current content and clear source
                content.MoveTo(builder);

                return content.SetHtmlContent(builder);
            }
        }

        /// <summary>
        /// Prepends <paramref name="encoded"/> to the existing content. <paramref name="encoded"/> is assumed
        /// to be an HTML encoded <see cref="string"/> and no further encoding will be performed.
        /// </summary>
        /// <param name="encoded">The <see cref="string"/> to be prepended.</param>
        /// <returns>A reference to this instance after the prepend operation has completed.</returns>
        public static TagHelperContent PrependHtml(this TagHelperContent content, string encoded)
        {
            if (content.IsEmptyOrWhiteSpace)
            {
                content.SetHtmlContent(encoded);
                content.AppendLine();
            }
            else
            {
                var builder = new HtmlContentBuilder(2);
                // Add new content first
                builder.AppendHtml(encoded);
                // Then append current content and clear source
                content.MoveTo(builder);

                content.SetHtmlContent(builder);
            }

            return content;
        }

        /// <summary>
        /// Prepends <paramref name="htmlContent"/> to the existing content.
        /// </summary>
        /// <param name="htmlContent">The <see cref="IHtmlContent"/> to be prepended.</param>
        /// <returns>A reference to this instance after the prepend operation has completed.</returns>
        public static TagHelperContent PrependHtml(this TagHelperContent content, IHtmlContent htmlContent)
        {
            if (content.IsEmptyOrWhiteSpace)
            {
                content.SetHtmlContent(htmlContent);
                content.AppendLine();
            }
            else
            {
                var builder = new HtmlContentBuilder(2);
                // Add new content first
                builder.AppendHtml(htmlContent);
                // Then append current content and clear source
                content.MoveTo(builder);

                content.SetHtmlContent(builder);
            }

            return content;
        }

        /// <summary>
        /// Prepends <see cref="output"/> to the current content of <see cref="content"/>
        /// </summary>
        public static void Prepend(this TagHelperContent content, TagHelperOutput source)
        {
            content.PrependHtml(source.ToTagHelperContent());
        }

        /// <summary>
        /// Wraps <see cref="tagBuilder"/> around <see cref="content"/>. <see cref="TagBuilder.InnerHtml"/> will be ignored.
        /// </summary>
        public static TagHelperContent WrapWith(this TagHelperContent content, TagBuilder tagBuilder)
        {
            content.PrependHtml(tagBuilder.RenderStartTag());
            content.AppendHtml(tagBuilder.RenderEndTag());

            return content;
        }

        /// <summary>
        /// Wraps <see cref="unencodedStart"/> and <see cref="unencodedEnd"/> around <see cref="content"/>
        /// </summary>
        public static void Wrap(TagHelperContent content, string unencodedStart, string unencodedEnd)
        {
            content.Prepend(unencodedStart);
            content.Append(unencodedEnd);
        }

        /// <summary>
        /// Wraps HTML encoded <see cref="encodedStart"/> and <see cref="encodedEnd"/> around <see cref="content"/>.
        /// No further encoding will be performed.
        /// </summary>
        public static void WrapHtml(TagHelperContent content, string encodedStart, string encodedEnd)
        {
            content.PrependHtml(encodedStart);
            content.AppendHtml(encodedEnd);
        }
    }
}