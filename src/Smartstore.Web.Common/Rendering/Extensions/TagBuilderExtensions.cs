using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Web.Rendering;

public static class TagBuilderExtensions {
    extension(TagBuilder builder)
    {
        public TagBuilder AppendCssClass(Func<string> cssClass)
        {
            builder.Attributes.AddInValue("class", ' ', cssClass(), false);
            return builder;
        }

        public TagBuilder PrependCssClass(Func<string> cssClass)
        {
            builder.Attributes.AddInValue("class", ' ', cssClass(), true);
            return builder;
        }

        public TagBuilder AppendCssClass(string cssClass)
        {
            builder.Attributes.AddInValue("class", ' ', cssClass, false);
            return builder;
        }

        public TagBuilder PrependCssClass(string cssClass)
        {
            builder.Attributes.AddInValue("class", ' ', cssClass, true);
            return builder;
        }

        /// <summary>
        /// Creates a DOM-like CSS class list object. Call 'Dispose()' to flush
        /// the result back to <paramref name="builder"/>.
        /// </summary>
        public CssClassList GetClassList()
        {
            return new CssClassList(builder.Attributes);
        }

        public void AddCssStyle(string expression, object value)
        {
            Guard.NotEmpty(expression);
            Guard.NotNull(value);

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

        public void AddCssStyles(string styles)
        {
            Guard.NotEmpty(styles);

            if (builder.Attributes.TryGetValue("style", out var str))
            {
                builder.Attributes["style"] = styles.EnsureEndsWith("; ") + str;
            }
            else
            {
                builder.Attributes["style"] = styles;
            }
        }

        public bool MergeAttribute(string key, string value, bool replaceExisting, bool ignoreNull)
        {
            if (value == null && ignoreNull)
            {
                return false;
            }

            builder.MergeAttribute(key, value, replaceExisting);
            return true;
        }

        public bool MergeAttribute(string key, Func<string> valueAccessor, bool replaceExisting, bool ignoreNull)
        {
            Guard.NotEmpty(key);
            Guard.NotNull(valueAccessor);

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
