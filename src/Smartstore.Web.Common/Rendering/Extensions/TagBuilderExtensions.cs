using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Web.Rendering
{
    public static class TagBuilderExtensions
    {
        public static void AppendCssClass(this TagBuilder builder, Func<string> cssClass)
        {
            builder.Attributes.AddInValue("class", ' ', cssClass(), false);
        }

        public static void PrependCssClass(this TagBuilder builder, Func<string> cssClass)
        {
            builder.Attributes.AddInValue("class", ' ', cssClass(), true);
        }

        public static void AppendCssClass(this TagBuilder builder, string cssClass)
        {
            builder.Attributes.AddInValue("class", ' ', cssClass, false);
        }

        public static void PrependCssClass(this TagBuilder builder, string cssClass)
        {
            builder.Attributes.AddInValue("class", ' ', cssClass, true);
        }

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

        public static bool MergeAttribute(this TagBuilder builder, string key, string value, bool replaceExisting, bool ignoreNull)
        {
            if (value == null && ignoreNull)
            {
                return false;
            }

            builder.MergeAttribute(key, value, replaceExisting);
            return true;
        }

        public static bool MergeAttribute(this TagBuilder builder, string key, Func<string> valueAccessor, bool replaceExisting, bool ignoreNull)
        {
            Guard.NotEmpty(key, nameof(key));
            Guard.NotNull(valueAccessor, nameof(valueAccessor));

            if (replaceExisting || !builder.Attributes.ContainsKey(key))
            {
                var value = valueAccessor();
                if (value != null || !ignoreNull)
                {
                    builder.Attributes[key] = value;
                    return true;
                }
            }

            return false;
        }
    }
}
