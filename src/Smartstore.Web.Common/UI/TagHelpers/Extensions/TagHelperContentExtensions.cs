using System;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.UI.TagHelpers
{
    public static class TagHelperContentExtensions
    {
        /// <summary>
        /// Prepends <see cref="value"/> to the current content of <see cref="content"/>
        /// </summary>
        public static void Prepend(this TagHelperContent content, string value)
        {
            if (content.IsEmptyOrWhiteSpace)
                content.SetContent(value);
            else
                content.SetContent(value + content.GetContent());
        }

        /// <summary>
        /// Prepends <see cref="value"/> to the current content of <see cref="content"/> without encoding it
        /// </summary>
        public static void PrependHtml(this TagHelperContent content, string value)
        {
            if (content.IsEmptyOrWhiteSpace)
                content.SetHtmlContent(value);
            else
                content.SetHtmlContent(value + content.GetContent());
        }

        /// <summary>
        /// Prepends <see cref="value"/> to the current content of <see cref="content"/> without encoding it
        /// </summary>
        public static void Prepend(this TagHelperContent content, IHtmlContent value)
        {
            if (content.IsEmptyOrWhiteSpace)
            {
                content.SetHtmlContent(value);
                content.AppendLine();
            }
            else
            {
                content.SetHtmlContent(value);
                content.AppendHtml(content.GetContent());
            }
        }

        /// <summary>
        /// Prepends <see cref="output"/> to the current content of <see cref="content"/>
        /// </summary>
        public static void Prepend(this TagHelperContent content, TagHelperOutput output)
        {
            content.Prepend(output.ToTagHelperContent());
        }

        /// <summary>
        /// Wraps <see cref="builder"/> around <see cref="content"/>. <see cref="TagBuilder.InnerHtml"/> will be ignored.
        /// </summary>
        public static void Wrap(this TagHelperContent content, TagBuilder builder)
        {
            builder.TagRenderMode = TagRenderMode.StartTag;
            Wrap(content, builder, new TagBuilder(builder.TagName) { TagRenderMode = TagRenderMode.EndTag });
        }

        /// <summary>
        /// Wraps <see cref="contentStart"/> and <see cref="contentEnd"/> around <see cref="content"/>
        /// </summary>
        public static void Wrap(TagHelperContent content, IHtmlContent contentStart, IHtmlContent contentEnd)
        {
            content.Prepend(contentStart);
            content.AppendHtml(contentEnd);
        }

        /// <summary>
        /// Wraps <see cref="contentStart"/> and <see cref="contentEnd"/> around <see cref="content"/>
        /// </summary>
        public static void Wrap(TagHelperContent content, string contentStart, string contentEnd)
        {
            content.Prepend(contentStart);
            content.Append(contentEnd);
        }

        /// <summary>
        /// Wraps <see cref="contentStart"/> and <see cref="contentEnd"/> around <see cref="content"/> without encoding it
        /// </summary>
        public static void WrapHtml(TagHelperContent content, string contentStart, string contentEnd)
        {
            content.PrependHtml(contentStart);
            content.AppendHtml(contentEnd);
        }
    }
}