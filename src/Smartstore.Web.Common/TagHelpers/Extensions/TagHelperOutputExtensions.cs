using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers
{
    /// <summary>
    /// <see cref="TagBuilder.InnerHtml"/> position when wrapping an element.
    /// </summary>
    public enum InnerHtmlPosition
    {
        /// <summary>
        /// Don't include inner HTML in the wrapping command.
        /// </summary>
        Exclude,
        /// <summary>
        /// Prepend inner HTML to the target element or content.
        /// </summary>
        Prepend,
        /// <summary>
        /// Append inner HTML to the target element or content.
        /// </summary>
        Append
    }
    
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
            Guard.NotEmpty(expression);
            Guard.NotNull(value);

            var style = expression + ": " + Convert.ToString(value, CultureInfo.InvariantCulture);
            AddCssStyles(output, style);
        }

        public static void AddCssStyles(this TagHelperOutput output, string styles)
        {
            Guard.NotEmpty(styles);

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
            Guard.NotEmpty(name);

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
            Guard.NotNull(output);
            Guard.NotNull(destination);

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
            Guard.NotNull(output);
            Guard.NotNull(destination);

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

        /// <inheritdoc cref="WrapElementWith(TagHelperOutput, InnerHtmlPosition, TagBuilder[])()" />
        public static TagHelperOutput WrapElementWith(this TagHelperOutput output, params TagBuilder[] tags)
        {
            return InternalWrapWith(Guard.NotNull(output), true, InnerHtmlPosition.Prepend, tags);
        }

        /// <summary>
        /// Wraps a series of tags around the element, where the first tag will be rendered as the outermost parent,
        /// and the last one as the direct element parent.
        /// </summary>
        /// <param name="tags">The tags to wrap the element with.</param>
        /// <param name="innerHtmlPosition">Specifies where the inner HTML of <paramref name="tags"/> should be placed in relation to the wrapped output.</param>
        public static TagHelperOutput WrapElementWith(this TagHelperOutput output, InnerHtmlPosition innerHtmlPosition, params TagBuilder[] tags)
        {
            return InternalWrapWith(Guard.NotNull(output), true, innerHtmlPosition, tags);
        }

        /// <inheritdoc cref="WrapContentWith(TagHelperOutput, InnerHtmlPosition, TagBuilder[])()" />
        public static TagHelperOutput WrapContentWith(this TagHelperOutput output, params TagBuilder[] tags)
        {
            return InternalWrapWith(Guard.NotNull(output), false, InnerHtmlPosition.Prepend, tags);
        }

        /// <summary>
        /// Wraps a series of tags around the element's inner content, where the first tag will be rendered as the outermost parent,
        /// and the last one as the direct content parent.
        /// </summary>
        /// <param name="tags">The tags to wrap the content with.</param>
        /// <param name="innerHtmlPosition">Specifies where the inner HTML of <paramref name="tags"/> should be placed in relation to the wrapped output.</param>
        public static TagHelperOutput WrapContentWith(this TagHelperOutput output, InnerHtmlPosition innerHtmlPosition, params TagBuilder[] tags)
        {
            return InternalWrapWith(Guard.NotNull(output), false, innerHtmlPosition, tags);
        }

        private static TagHelperOutput InternalWrapWith(TagHelperOutput output, bool wrapElement, InnerHtmlPosition innerHtmlPosition, TagBuilder[] tags)
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
                if (tags[i].HasInnerHtml && innerHtmlPosition != InnerHtmlPosition.Exclude)
                {
                    (innerHtmlPosition == InnerHtmlPosition.Prepend ? pre : post).AppendHtml(tags[i].RenderBody());
                }
            }

            for (var i = tags.Length - 1; i >= 0; i--)
            {
                post.AppendHtml(tags[i].RenderEndTag());
            }

            return output;
        }

        /// <summary>
        ///     Wraps a <see cref="tag" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />. All content that is
        ///     inside the <see cref="output" /> will be inside of the <see cref="tag" />.
        /// </summary>
        /// <param name="tag">The tag to wrap the content with.</param>
        /// <param name="innerHtmlPosition">Specifies where the inner HTML of <paramref name="tag"/> should be placed in relation to the wrapping tag.</param>
        public static TagHelperOutput WrapContentOutside(this TagHelperOutput output, TagBuilder tag, InnerHtmlPosition innerHtmlPosition = InnerHtmlPosition.Exclude)
        {
            output.PreContent.PrependHtml(tag.RenderStartTag());

            if (tag.HasInnerHtml)
            {
                if (innerHtmlPosition == InnerHtmlPosition.Prepend)
                {
                    output.PreContent.AppendHtml(tag.RenderBody());
                }
                else if (innerHtmlPosition == InnerHtmlPosition.Append)
                {
                    output.PostContent.AppendHtml(tag.RenderBody());
                }
            }

            output.PostContent.AppendHtml(tag.RenderEndTag());

            return output;
        }

        /// <summary>
        ///     Wraps <see cref="startContent" /> and <see cref="endContent" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />. All content that is
        ///     inside the <see cref="output" /> will be inside of the <see cref="string" />s.
        /// </summary>
        public static TagHelperOutput WrapContentOutside(this TagHelperOutput output, string startContent, string endContent)
        {
            output.PreContent.Prepend(startContent);
            output.PostContent.Append(endContent);

            return output;
        }

        /// <summary>
        ///     Wraps <see cref="startContent" /> and <see cref="endContent" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />. All content that is
        ///     inside the <see cref="output" /> will be inside of the <see cref="string" />s. <see cref="startContent" /> and
        ///     <see cref="endContent" /> will not be encoded.
        /// </summary>
        public static TagHelperOutput WrapHtmlContentOutside(this TagHelperOutput output, string startContent, string endContent)
        {
            output.PreContent.PrependHtml(startContent);
            output.PostContent.AppendHtml(endContent);

            return output;
        }

        /// <summary>
        ///     Wraps a <see cref="tag" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />. The current contents of
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" /> will be outside.
        /// </summary>
        /// <param name="tag">The tag to wrap the content with.</param>
        public static TagHelperOutput WrapContentInside(this TagHelperOutput output, TagBuilder tag)
        {
            output.PreContent.AppendHtml(tag.RenderStartTag());
            output.PreContent.AppendHtml(tag.RenderBody());
            output.PostContent.PrependHtml(tag.RenderEndTag());

            return output;
        }

        /// <summary>
        ///     Wraps <see cref="startContent" /> and <see cref="endContent" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />.
        ///     The current contents of <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" /> will be outside.
        /// </summary>
        public static TagHelperOutput WrapContentInside(this TagHelperOutput output, string startContent, string endContent)
        {
            output.PreContent.Append(startContent);
            output.PostContent.Prepend(endContent);

            return output;
        }

        /// <summary>
        ///     Wraps <see cref="startContent" /> and <see cref="endTag" /> around the content of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" />.
        ///     The current contents of <see cref="TagHelperOutput.PreContent" /> and <see cref="TagHelperOutput.PostContent" /> will be outside.
        ///     <see cref="startContent" /> and <see cref="endContent" /> will not be encoded.
        /// </summary>
        public static TagHelperOutput WrapHtmlContentInside(this TagHelperOutput output, string startContent, string endContent)
        {
            output.PreContent.AppendHtml(startContent);
            output.PostContent.PrependHtml(endContent);

            return output;
        }

        /// <summary>
        ///     Wraps a <see cref="tag" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be inside.
        /// </summary>
        /// <param name="tag">The tag to wrap the element with.</param>
        /// <param name="innerHtmlPosition">Specifies where the inner HTML of <paramref name="tag"/> should be placed in relation to the wrapping tag.</param>
        public static TagHelperOutput WrapOutside(this TagHelperOutput output, TagBuilder tag, InnerHtmlPosition innerHtmlPosition = InnerHtmlPosition.Exclude)
        {
            output.PreElement.PrependHtml(tag.RenderStartTag());

            if (tag.HasInnerHtml)
            {
                if (innerHtmlPosition == InnerHtmlPosition.Prepend)
                {
                    output.PreElement.AppendHtml(tag.RenderBody());
                }
                else if (innerHtmlPosition == InnerHtmlPosition.Append)
                {
                    output.PostElement.AppendHtml(tag.RenderBody());
                }
            }

            output.PostElement.AppendHtml(tag.RenderEndTag());

            return output;
        }

        /// <summary>
        ///     Wraps <see cref="startContent" /> and <see cref="endContent" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be inside.
        /// </summary>
        public static TagHelperOutput WrapOutside(this TagHelperOutput output, string startContent, string endContent)
        {
            output.PreElement.Prepend(startContent);
            output.PostElement.Append(endContent);

            return output;
        }

        /// <summary>
        ///     Wraps <see cref="startContent" /> and <see cref="endContent" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be inside.
        ///     <see cref="startContent" /> and <see cref="endContent" /> will not be encoded.
        /// </summary>
        public static TagHelperOutput WrapHtmlOutside(this TagHelperOutput output, string startContent, string endContent)
        {
            output.PreElement.PrependHtml(startContent);
            output.PostElement.AppendHtml(endContent);

            return output;
        }

        /// <summary>
        ///     Wraps a <see cref="tag" /> around the element of the <see cref="output" /> using
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
        ///     Wraps <see cref="startContent" /> and <see cref="endContent" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be outside.
        /// </summary>
        public static TagHelperOutput WrapInside(this TagHelperOutput output, string startContent, string endContent)
        {
            output.PreElement.Append(startContent);
            output.PostElement.Prepend(endContent);

            return output;
        }

        /// <summary>
        ///     Wraps <see cref="startContent" /> and <see cref="endContent" /> around the element of the <see cref="output" /> using
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" />. The current contents of
        ///     <see cref="TagHelperOutput.PreElement" /> and <see cref="TagHelperOutput.PostElement" /> will be outside.
        ///     <see cref="startContent" /> and <see cref="endContent" /> will not be encoded.
        /// </summary>
        public static TagHelperOutput WrapHtmlInside(this TagHelperOutput output, string startContent, string endContent)
        {
            output.PreElement.AppendHtml(startContent);
            output.PostElement.PrependHtml(endContent);

            return output;
        }

        #endregion
    }
}