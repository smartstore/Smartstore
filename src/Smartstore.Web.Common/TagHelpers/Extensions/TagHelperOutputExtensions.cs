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

        /// <summary>
        /// Wraps a series of tags around the PRE and POST element.
        /// The <see cref="TagBuilder.InnerHtml"/> will be rendered right after the opening tag.
        /// </summary>
        /// <inheritdoc cref="InternalWrapWith(TagHelperOutput, TagBuilder[], InnerHtmlPosition, bool, bool)" />
        public static TagHelperOutput WrapElementWith(this TagHelperOutput output, params TagBuilder[] tags)
            => InternalWrapWith(output, tags, InnerHtmlPosition.Prepend, wrapElement: true, inside: false);

        /// <summary>
        /// Wraps a series of tags around the element.
        /// The <see cref="TagBuilder.InnerHtml"/> will be rendered right after the opening tag.
        /// </summary>
        /// <inheritdoc cref="InternalWrapWith(TagHelperOutput, TagBuilder[], InnerHtmlPosition, bool, bool)" />
        public static TagHelperOutput WrapElementInsideWith(this TagHelperOutput output, params TagBuilder[] tags)
            => InternalWrapWith(output, tags, InnerHtmlPosition.Prepend, wrapElement: true, inside: true);

        /// <summary>
        /// Wraps a series of tags around the PRE and POST element.
        /// </summary>
        /// <inheritdoc cref="InternalWrapWith(TagHelperOutput, TagBuilder[], InnerHtmlPosition, bool, bool)" />
        public static TagHelperOutput WrapElementWith(this TagHelperOutput output, InnerHtmlPosition innerHtmlPosition, params TagBuilder[] tags)
            => InternalWrapWith(output, tags, innerHtmlPosition, wrapElement: true, inside: false);

        /// <summary>
        /// Wraps a series of tags around the element.
        /// </summary>
        /// <inheritdoc cref="InternalWrapWith(TagHelperOutput, TagBuilder[], InnerHtmlPosition, bool, bool)" />
        public static TagHelperOutput WrapElementInsideWith(this TagHelperOutput output, InnerHtmlPosition innerHtmlPosition, params TagBuilder[] tags)
            => InternalWrapWith(output, tags, innerHtmlPosition, wrapElement: true, inside: true);

        /// <summary>
        /// Wraps a series of tags around the element's child PRE and POST content.
        /// The <see cref="TagBuilder.InnerHtml"/> will be rendered right after the opening tag.
        /// </summary>
        /// <inheritdoc cref="InternalWrapWith(TagHelperOutput, TagBuilder[], InnerHtmlPosition, bool, bool)" />
        public static TagHelperOutput WrapContentWith(this TagHelperOutput output, params TagBuilder[] tags)
            => InternalWrapWith(output, tags, InnerHtmlPosition.Prepend, wrapElement: false, inside: false);

        /// <summary>
        /// Wraps a series of tags around the element's child content.
        /// The <see cref="TagBuilder.InnerHtml"/> will be rendered right after the opening tag.
        /// </summary>
        /// <inheritdoc cref="InternalWrapWith(TagHelperOutput, TagBuilder[], InnerHtmlPosition, bool, bool)" />
        public static TagHelperOutput WrapContentInsideWith(this TagHelperOutput output, params TagBuilder[] tags)
            => InternalWrapWith(output, tags, InnerHtmlPosition.Prepend, wrapElement: false, inside: true);

        /// <summary>
        /// Wraps a series of tags around the element's child PRE and POST content.
        /// </summary>
        /// <inheritdoc cref="InternalWrapWith(TagHelperOutput, TagBuilder[], InnerHtmlPosition, bool, bool)" />
        public static TagHelperOutput WrapContentWith(this TagHelperOutput output, InnerHtmlPosition innerHtmlPosition, params TagBuilder[] tags)
            => InternalWrapWith(output, tags, innerHtmlPosition, wrapElement: false, inside: false);

        /// <summary>
        /// Wraps a series of tags around the element's child content.
        /// </summary>
        /// <inheritdoc cref="InternalWrapWith(TagHelperOutput, TagBuilder[], InnerHtmlPosition, bool, bool)" />
        public static TagHelperOutput WrapContentInsideWith(this TagHelperOutput output, InnerHtmlPosition innerHtmlPosition, params TagBuilder[] tags)
            => InternalWrapWith(output, tags, innerHtmlPosition, wrapElement: false, inside: true);

        /// <summary>
        /// Wraps a series of tags around the content.
        /// </summary>
        /// <remarks>
        /// The first tag will be rendered as the outermost parent, and the last one as the direct content parent.
        /// </remarks>
        /// <param name="tags">The tags to wrap the output with.</param>
        /// <param name="innerHtmlPosition">Specifies where the inner HTML of <paramref name="tags"/> should be placed in relation to the wrapped output.</param>
        private static TagHelperOutput InternalWrapWith(
            TagHelperOutput output,
            TagBuilder[] tags,
            InnerHtmlPosition innerHtmlPosition,
            bool wrapElement, // Wraps content otherwise
            bool inside) // Wraps outside otherwise (inside = After Pre / Before Post, outside = Before Pre / After Post)
        {
            Guard.NotNull(output);

            if (tags.Length == 0)
            {
                return output;
            }

            var openingContainer = new HtmlContentBuilder();
            var closingContainer = new HtmlContentBuilder();

            // Build container for "start tags".
            // --> <tag1><tag2><tag3>
            for (var i = 0; i < tags.Length; i++)
            {
                openingContainer.AppendHtml(tags[i].RenderStartTag());
                if (tags[i].HasInnerHtml && innerHtmlPosition == InnerHtmlPosition.Prepend)
                {
                    // --> <tag1><span>inner</span> [...]
                    openingContainer.AppendHtml(tags[i].RenderBody());
                }
            }

            // Append/Prepend start container to the output.
            if (inside)
            {
                if (wrapElement) 
                    output.PreElement.AppendHtml(openingContainer); 
                else 
                    output.PreContent.AppendHtml(openingContainer);
            }
            else
            {
                if (wrapElement) 
                    output.PreElement.PrependHtml(openingContainer); 
                else 
                    output.PreContent.PrependHtml(openingContainer);
            }

            // Build container for "end tags", now vice versa.
            // --> </tag3></tag2></tag1>
            for (var i = tags.Length - 1; i >= 0; i--)
            {
                if (tags[i].HasInnerHtml && innerHtmlPosition == InnerHtmlPosition.Append)
                {
                    // --> [...] <span>inner</span></tag3>
                    closingContainer.AppendHtml(tags[i].RenderBody());
                }
                closingContainer.AppendHtml(tags[i].RenderEndTag());
            }

            // Append/Prepend end container to the output.
            if (inside)
            {
                if (wrapElement) 
                    output.PostElement.PrependHtml(closingContainer); 
                else 
                    output.Content.AppendHtml(closingContainer);
            }
            else
            {
                if (wrapElement) 
                    output.PostElement.AppendHtml(closingContainer); 
                else 
                    output.PostContent.AppendHtml(closingContainer);
            }

            return output;
        }

        #endregion
    }
}