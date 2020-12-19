using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.UI.TagHelpers
{
    public static class TagHelperOutputExtensions
    {
        /// <summary>
        /// Adds a style entry
        /// </summary>
        public static void AddCssStyle(this TagHelperOutput output, string name, string value)
        {
            if (output.Attributes.ContainsName("style"))
            {
                var styleValue = output.Attributes["style"].Value.ToString();
                if (styleValue.IsEmpty())
                {
                    output.Attributes.SetAttribute("style", name + ": " + value + ";");
                }
                else
                {
                    output.Attributes.SetAttribute("style", (styleValue.EndsWith(";")
                                                             ? " "
                                                             : "; ") + name + ": " + value + ";");
                }
            }
            else
            {
                output.Attributes.Add("style", name + ": " + value + ";");
            }
        }

        /// <summary>
        /// Loads the child content asynchronously.
        /// </summary>
        public static async Task LoadAndSetChildContentAsync(this TagHelperOutput output)
        {
            output.Content.SetHtmlContent(await output.GetChildContentAsync() ?? new DefaultTagHelperContent());
        }

        /// <summary>
        /// Converts a <see cref="output" /> into a <see cref="TagHelperContent" />
        /// </summary>
        public static TagHelperContent ToTagHelperContent(this TagHelperOutput output)
        {
            var content = new DefaultTagHelperContent();
            content.AppendHtml(output.PreElement);
            var builder = new TagBuilder(output.TagName);

            foreach (var attribute in output.Attributes)
            {
                builder.Attributes.Add(attribute.Name, attribute.Value?.ToString());
            }

            if (output.TagMode == TagMode.SelfClosing)
            {
                builder.TagRenderMode = TagRenderMode.SelfClosing;
                content.AppendHtml(builder);
            }
            else
            {
                builder.TagRenderMode = TagRenderMode.StartTag;
                content.AppendHtml(builder);
                content.AppendHtml(output.PreContent);
                content.AppendHtml(output.Content);
                content.AppendHtml(output.PostContent);

                if (output.TagMode == TagMode.StartTagAndEndTag)
                {
                    content.AppendHtml($"</{output.TagName}>");
                }
            }

            content.AppendHtml(output.PostElement);
            return content;
        }

        /// <summary>
        ///     Wraps a <see cref="builder" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />. All content that is
        ///     inside the <see cref="output" /> will be inside of the <see cref="builder" />.
        ///     <see cref="TagBuilder.InnerHtml" /> will not be included.
        /// </summary>
        public static void WrapContentOutside(this TagHelperOutput output, TagBuilder builder)
        {
            builder.TagRenderMode = TagRenderMode.StartTag;
            WrapContentOutside(output, builder, new TagBuilder(builder.TagName) { TagRenderMode = TagRenderMode.EndTag });
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />. All content that is
        ///     inside the <see cref="output" /> will be inside of the <see cref="Microsoft.AspNetCore.Html.IHtmlContent" />s.
        /// </summary>
        public static void WrapContentOutside(this TagHelperOutput output, IHtmlContent startTag, IHtmlContent endTag)
        {
            output.PreContent.Prepend(startTag);
            output.PostContent.AppendHtml(endTag);
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />. All content that is
        ///     inside the <see cref="output" /> will be inside of the <see cref="string" />s.
        /// </summary>
        public static void WrapContentOutside(this TagHelperOutput output, string startTag, string endTag)
        {
            output.PreContent.Prepend(startTag);
            output.PostContent.Append(endTag);
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />. All content that is
        ///     inside the <see cref="output" /> will be inside of the <see cref="string" />s. <see cref="startTag" /> and
        ///     <see cref="endTag" /> will not be encoded.
        /// </summary>
        public static void WrapHtmlContentOutside(this TagHelperOutput output, string startTag, string endTag)
        {
            output.PreContent.PrependHtml(startTag);
            output.PostContent.AppendHtml(endTag);
        }

        /// <summary>
        ///     Wraps a <see cref="builder" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />. The current contents of
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" /> will be outside.
        /// </summary>
        public static void WrapContentInside(this TagHelperOutput output, TagBuilder builder)
        {
            builder.TagRenderMode = TagRenderMode.StartTag;
            WrapContentInside(output, builder, new TagBuilder(builder.TagName) { TagRenderMode = TagRenderMode.EndTag });
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />.
        ///     <see cref="TagBuilder.InnerHtml" /> will not be included. The current contents of
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" /> will be outside.
        /// </summary>
        public static void WrapContentInside(this TagHelperOutput output, IHtmlContent startTag, IHtmlContent endTag)
        {
            output.PreContent.AppendHtml(startTag);
            output.PostContent.Prepend(endTag);
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />.
        ///     <see cref="TagBuilder.InnerHtml" /> will not be included. The current contents of
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" /> will be outside.
        /// </summary>
        public static void WrapContentInside(this TagHelperOutput output, string startTag, string endTag)
        {
            output.PreContent.Append(startTag);
            output.PostContent.Prepend(endTag);
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />.
        ///     <see cref="TagBuilder.InnerHtml" /> will not be included. The current contents of
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" /> will be outside.
        ///     <see cref="startTag" /> and <see cref="endTag" /> will not be encoded.
        /// </summary>
        public static void WrapHtmlContentInside(this TagHelperOutput output, string startTag, string endTag)
        {
            output.PreContent.AppendHtml(startTag);
            output.PostContent.PrependHtml(endTag);
        }

        /// <summary>
        ///     Wraps a <see cref="builder" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be inside.
        ///     <see cref="TagBuilder.InnerHtml" /> will not be included.
        /// </summary>
        public static void WrapOutside(this TagHelperOutput output, TagBuilder builder)
        {
            builder.TagRenderMode = TagRenderMode.StartTag;
            WrapOutside(output, builder, new TagBuilder(builder.TagName) { TagRenderMode = TagRenderMode.EndTag });
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be inside.
        /// </summary>
        public static void WrapOutside(this TagHelperOutput output, IHtmlContent startTag, IHtmlContent endTag)
        {
            output.PreElement.Prepend(startTag);
            output.PostElement.AppendHtml(endTag);
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be inside.
        /// </summary>
        public static void WrapOutside(this TagHelperOutput output, string startTag, string endTag)
        {
            output.PreElement.Prepend(startTag);
            output.PostElement.Append(endTag);
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be inside.
        ///     <see cref="startTag" /> and <see cref="endTag" /> will not be encoded.
        /// </summary>
        public static void WrapHtmlOutside(this TagHelperOutput output, string startTag, string endTag)
        {
            output.PreElement.PrependHtml(startTag);
            output.PostElement.AppendHtml(endTag);
        }

        /// <summary>
        ///     Wraps a <see cref="builder" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be outside.
        ///     <see cref="TagBuilder.InnerHtml" /> will not be included.
        /// </summary>
        public static void WrapInside(this TagHelperOutput output, TagBuilder builder)
        {
            builder.TagRenderMode = TagRenderMode.StartTag;
            WrapInside(output, builder, new TagBuilder(builder.TagName) { TagRenderMode = TagRenderMode.EndTag });
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be Outside.
        /// </summary>
        public static void WrapInside(this TagHelperOutput output, IHtmlContent startTag, IHtmlContent endTag)
        {
            output.PreElement.AppendHtml(startTag);
            output.PostElement.Prepend(endTag);
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be outside.
        /// </summary>
        public static void WrapInside(this TagHelperOutput output, string startTag, string endTag)
        {
            output.PreElement.Append(startTag);
            output.PostElement.Prepend(endTag);
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be outside.
        ///     <see cref="startTag" /> and <see cref="endTag" /> will not be encoded.
        /// </summary>
        public static void WrapHtmlInside(this TagHelperOutput output, string startTag, string endTag)
        {
            output.PreElement.AppendHtml(startTag);
            output.PostElement.PrependHtml(endTag);
        }
    }
}