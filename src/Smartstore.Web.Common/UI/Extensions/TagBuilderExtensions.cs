using System;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Web.UI
{
    public static class TagBuilderExtensions
    {
        /// <summary>
        /// Creates a DOM-like CSS class list object. Call 'Dispose()' to flush
        /// the result back to <paramref name="builder"/>.
        /// </summary>
        public static CssClassList GetClassList(this TagBuilder builder)
        {
            return new CssClassList(builder.Attributes);
        }

        public static void AddCssStyle(this TagBuilder builder, string expression, object value)
        {
            Guard.NotEmpty(expression, nameof(expression));
            Guard.NotNull(value, nameof(value));

            var style = expression + ": " + Convert.ToString(value, CultureInfo.InvariantCulture);

            if (builder.Attributes.TryGetValue("style", out var str))
            {
                builder.Attributes["style"] = style + "; " + str;
            }
            else
            {
                builder.Attributes["style"] = style;
            }
        }

        public static void AddCssStyles(this TagBuilder builder, string styles)
        {
            Guard.NotEmpty(styles, nameof(styles));

            if (builder.Attributes.TryGetValue("style", out var str))
            {
                builder.Attributes["style"] = styles.EnsureEndsWith("; ") + str;
            }
            else
            {
                builder.Attributes["style"] = styles;
            }
        }
    }
}
