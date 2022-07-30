using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers
{
    public static class TagHelperOutputExtensions
    {
        #region CSS

        /// <summary>
        /// Creates a DOM-like CSS class list object. Call 'Dispose()' to flush
        /// the result back to <paramref name="output"/>.
        /// </summary>
        public static CssClassList GetClassList(this TagHelperOutput output)
        {
            return new CssClassList(output.Attributes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendCssClass(this TagHelperOutput output, string cssClass)
        {
            AddInAttributeValue(output.Attributes, "class", ' ', cssClass, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrependCssClass(this TagHelperOutput output, string cssClass)
        {
            AddInAttributeValue(output.Attributes, "class", ' ', cssClass, true);
        }

        private static void AddInAttributeValue(TagHelperAttributeList attributes, string name, char separator, string value, bool prepend = false)
        {
            if (!attributes.TryGetAttribute(name, out var attribute))
            {
                attributes.Add(new TagHelperAttribute(name, value));
            }
            else
            {
                var currentValue = attribute.ValueAsString();

                if (DictionaryExtensions.TryAddInValue(value, currentValue, separator, prepend, out var mergedValue))
                {
                    attributes.SetAttribute(name, mergedValue);
                }
            }
        }

        public static void AddCssStyle(this TagHelperOutput output, string expression, object value)
        {
            Guard.NotEmpty(expression, nameof(expression));
            Guard.NotNull(value, nameof(value));

            var style = expression + ": " + Convert.ToString(value, CultureInfo.InvariantCulture);
            AddCssStyles(output, style);
        }

        public static void AddCssStyles(this TagHelperOutput output, string styles)
        {
            Guard.NotEmpty(styles, nameof(styles));

            if (output.Attributes.TryGetAttribute("style", out var attribute))
            {
                var currentStyle = attribute.ValueAsString().TrimEnd().EnsureEndsWith("; ");
                output.Attributes.SetAttribute("style", currentStyle + styles);
            }
            else
            {
                output.Attributes.SetAttribute("style", styles);
            }
        }

        #endregion

        #region Attributes

        public static void MergeAttribute(this TagHelperOutput output, string name, object value, bool replace = false)
        {
            Guard.NotEmpty(name, nameof(name));

            if (output.Attributes.ContainsName(name) && replace)
            {
                output.Attributes.SetAttribute(name, value);
            }
            else
            {
                output.Attributes.Add(name, value);
            }
        }

        #endregion

        #region Content

        /// <summary>
        /// Loads the child content asynchronously.
        /// </summary>
        public static async Task LoadAndSetChildContentAsync(this TagHelperOutput output)
        {
            output.Content.SetHtmlContent(await output.GetChildContentAsync() ?? new DefaultTagHelperContent());
        }

        /// <summary>
        /// Copies the complete HTML content of current <see cref="TagHelperOutput"/> to
        /// <paramref name="destination"/> output.
        /// </summary>
        public static void CopyTo(this TagHelperOutput output, IHtmlContentBuilder destination)
        {
            Guard.NotNull(output, nameof(output));
            Guard.NotNull(destination, nameof(destination));

            // Copy PreElement
            output.PreElement.CopyTo(destination);

            // Copy Element (Tag and Attributes).
            CopyElement(output, destination, false);

            // Copy PostElement
            output.PostElement.CopyTo(destination);
        }

        /// <summary>
        /// Moves the complete HTML content of current <see cref="TagHelperOutput"/> to
        /// <paramref name="destination"/> output.
        /// </summary>
        public static void MoveTo(this TagHelperOutput output, IHtmlContentBuilder destination)
        {
            Guard.NotNull(output, nameof(output));
            Guard.NotNull(destination, nameof(destination));

            // Copy PreElement
            output.PreElement.MoveTo(destination);

            // Copy Element (Tag and Attributes).
            CopyElement(output, destination, true);

            // Copy PostElement
            output.PostElement.MoveTo(destination);
        }

        /// <summary>
        /// Converts a <see cref="TagHelperOutput" /> to a <see cref="TagHelperContent" />
        /// </summary>
        public static TagHelperContent ToTagHelperContent(this TagHelperOutput output)
        {
            var content = new DefaultTagHelperContent();

            // Set PreElement
            content.AppendHtml(output.PreElement);

            // Copy Element (Tag and Attributes).
            CopyElement(output, content, false);

            // Set PostElement
            content.AppendHtml(output.PostElement);

            return content;
        }

        private static void CopyElement(TagHelperOutput output, IHtmlContentBuilder destination, bool move = false)
        {
            if (output.TagName == null)
            {
                return;
            }

            switch (output.TagMode)
            {
                case TagMode.StartTagOnly:
                    destination.AppendHtml("<");
                    destination.AppendHtml(output.TagName);
                    CopyAttributes(output, destination, move);
                    destination.AppendHtml(">");
                    break;
                case TagMode.SelfClosing:
                    destination.AppendHtml("<");
                    destination.AppendHtml(output.TagName);
                    CopyAttributes(output, destination, move);
                    destination.AppendHtml(" />");
                    break;
                default:
                    destination.AppendHtml("<");
                    destination.AppendHtml(output.TagName);
                    CopyAttributes(output, destination, move);
                    destination.AppendHtml(">");

                    // InnerHtml
                    if (move)
                    {
                        output.PreContent.MoveTo(destination);
                        output.Content.MoveTo(destination);
                        output.PostContent.MoveTo(destination);
                    }
                    else
                    {
                        output.PreContent.CopyTo(destination);
                        output.Content.CopyTo(destination);
                        output.PostContent.CopyTo(destination);
                    }

                    destination.AppendHtml("</");
                    destination.AppendHtml(output.TagName);
                    destination.AppendHtml(">");
                    break;
            }
        }

        private static void CopyAttributes(TagHelperOutput output, IHtmlContentBuilder destination, bool move = false)
        {
            if (output.Attributes.Count > 0)
            {
                foreach (var attr in output.Attributes)
                {
                    destination.Append(" ");
                    if (move)
                    {
                        attr.MoveTo(destination);
                    }
                    else
                    {
                        attr.CopyTo(destination);
                    }
                }
            }
        }

        #endregion

        #region Wrap

        /// <summary>
        /// Wraps a series of tags around the element, where the first tag will be rendered as the outermost parent,
        /// and the last one as the direct element parent.
        /// </summary>
        /// <param name="tags">The tags to wrap the element with.</param>
        public static TagHelperOutput WrapElementWith(this TagHelperOutput output, params TagBuilder[] tags)
        {
            Guard.NotNull(output, nameof(output));

            return WrapWithCore(output, true, tags);
        }

        /// <summary>
        /// Wraps a series of tags around the element's inner content, where the first tag will be rendered as the outermost parent,
        /// and the last one as the direct content parent.
        /// </summary>
        /// <param name="tags">The tags to wrap the content with.</param>
        public static TagHelperOutput WrapContentWith(this TagHelperOutput output, params TagBuilder[] tags)
        {
            Guard.NotNull(output, nameof(output));

            return WrapWithCore(output, false, tags);
        }

        private static TagHelperOutput WrapWithCore(TagHelperOutput output, bool wrapElement, TagBuilder[] tags)
        {
            if (tags.Length == 0)
            {
                return output;
            }

            var pre = wrapElement ? output.PreElement : output.PreContent;
            var post = wrapElement ? output.PostElement : output.PostContent;

            for (var i = 0; i < tags.Length; i++)
            {
                pre.AppendHtml(tags[i].RenderStartTag());
                if (tags[i].HasInnerHtml)
                {
                    pre.AppendHtml(tags[i].RenderBody());
                }
            }

            for (var i = tags.Length - 1; i >= 0; i--)
            {
                post.AppendHtml(tags[i].RenderEndTag());
            }

            return output;
        }

        /// <summary>
        ///     Wraps a <see cref="builder" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />. All content that is
        ///     inside the <see cref="output" /> will be inside of the <see cref="builder" />.
        ///     <see cref="TagBuilder.InnerHtml" /> will not be included.
        /// </summary>
        public static TagHelperOutput WrapContentOutside(this TagHelperOutput output, TagBuilder tag)
        {
            output.PreContent.PrependHtml(tag.RenderStartTag());
            output.PostContent.AppendHtml(tag.RenderEndTag());
            return output;
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />. All content that is
        ///     inside the <see cref="output" /> will be inside of the <see cref="string" />s.
        /// </summary>
        public static TagHelperOutput WrapContentOutside(this TagHelperOutput output, string startTag, string endTag)
        {
            output.PreContent.Prepend(startTag);
            output.PostContent.Append(endTag);
            return output;
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />. All content that is
        ///     inside the <see cref="output" /> will be inside of the <see cref="string" />s. <see cref="startTag" /> and
        ///     <see cref="endTag" /> will not be encoded.
        /// </summary>
        public static TagHelperOutput WrapHtmlContentOutside(this TagHelperOutput output, string startTag, string endTag)
        {
            output.PreContent.PrependHtml(startTag);
            output.PostContent.AppendHtml(endTag);
            return output;
        }

        /// <summary>
        ///     Wraps a <see cref="builder" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />. The current contents of
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" /> will be outside.
        /// </summary>
        public static TagHelperOutput WrapContentInside(this TagHelperOutput output, TagBuilder tag)
        {
            output.PreContent.AppendHtml(tag.RenderStartTag());
            output.PostContent.PrependHtml(tag.RenderEndTag());
            return output;
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />.
        ///     <see cref="TagBuilder.InnerHtml" /> will not be included. The current contents of
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" /> will be outside.
        /// </summary>
        public static TagHelperOutput WrapContentInside(this TagHelperOutput output, string startTag, string endTag)
        {
            output.PreContent.Append(startTag);
            output.PostContent.Prepend(endTag);
            return output;
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />.
        ///     <see cref="TagBuilder.InnerHtml" /> will not be included. The current contents of
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" /> will be outside.
        ///     <see cref="startTag" /> and <see cref="endTag" /> will not be encoded.
        /// </summary>
        public static TagHelperOutput WrapHtmlContentInside(this TagHelperOutput output, string startTag, string endTag)
        {
            output.PreContent.AppendHtml(startTag);
            output.PostContent.PrependHtml(endTag);
            return output;
        }

        /// <summary>
        ///     Wraps a <see cref="builder" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be inside.
        ///     <see cref="TagBuilder.InnerHtml" /> will not be included.
        /// </summary>
        public static TagHelperOutput WrapOutside(this TagHelperOutput output, TagBuilder tag)
        {
            output.PreElement.PrependHtml(tag.RenderStartTag());
            output.PostElement.AppendHtml(tag.RenderEndTag());
            return output;
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be inside.
        /// </summary>
        public static TagHelperOutput WrapOutside(this TagHelperOutput output, string startTag, string endTag)
        {
            output.PreElement.Prepend(startTag);
            output.PostElement.Append(endTag);
            return output;
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be inside.
        ///     <see cref="startTag" /> and <see cref="endTag" /> will not be encoded.
        /// </summary>
        public static TagHelperOutput WrapHtmlOutside(this TagHelperOutput output, string startTag, string endTag)
        {
            output.PreElement.PrependHtml(startTag);
            output.PostElement.AppendHtml(endTag);
            return output;
        }

        /// <summary>
        ///     Wraps a <see cref="builder" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be outside.
        ///     <see cref="TagBuilder.InnerHtml" /> will not be included.
        /// </summary>
        public static TagHelperOutput WrapInside(this TagHelperOutput output, TagBuilder tag)
        {
            output.PreElement.AppendHtml(tag.RenderStartTag());
            output.PostElement.PrependHtml(tag.RenderEndTag());
            return output;
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be outside.
        /// </summary>
        public static TagHelperOutput WrapInside(this TagHelperOutput output, string startTag, string endTag)
        {
            output.PreElement.Append(startTag);
            output.PostElement.Prepend(endTag);
            return output;
        }

        /// <summary>
        ///     Wraps <see cref="startTag" /> and <see cref="endTag" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be outside.
        ///     <see cref="startTag" /> and <see cref="endTag" /> will not be encoded.
        /// </summary>
        public static TagHelperOutput WrapHtmlInside(this TagHelperOutput output, string startTag, string endTag)
        {
            output.PreElement.AppendHtml(startTag);
            output.PostElement.PrependHtml(endTag);
            return output;
        }

        #endregion
    }
}