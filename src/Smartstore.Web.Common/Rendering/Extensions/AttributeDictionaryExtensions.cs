using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.Rendering
{
    public static class AttributeDictionaryExtensions
    {
        public static AttributeDictionary Merge(this AttributeDictionary attributes, string name, string value, bool replaceExisting = false)
        {
            Guard.NotNull(attributes, nameof(attributes));
            Guard.NotEmpty(name, nameof(name));

            if (replaceExisting || !attributes.ContainsKey(name))
            {
                attributes[name] = value;
            }

            return attributes;
        }

        public static AttributeDictionary Merge(this AttributeDictionary attributes, IDictionary<string, object> source, bool replaceExisting = false)
        {
            Guard.NotNull(attributes, nameof(attributes));

            if (source != null)
            {
                foreach (var kvp in source)
                {
                    if (replaceExisting || !attributes.ContainsKey(kvp.Key))
                    {
                        attributes[kvp.Key] = kvp.Value?.ToString();
                    }
                }
            }

            return attributes;
        }

        /// <summary>
        /// Copies all attributes from <paramref name="attributes"/> to <paramref name="target"/>
        /// overriding any existing attribute.
        /// </summary>
        public static void CopyTo(this AttributeDictionary attributes, TagHelperAttributeList target)
        {
            Guard.NotNull(attributes, nameof(attributes));
            Guard.NotNull(target, nameof(target));

            foreach (var attr in attributes)
            {
                target.SetAttribute(attr.Key, attr.Value);
            }
        }

        public static AttributeDictionary AppendCssClass(this AttributeDictionary attributes, Func<string> cssClass)
        {
            attributes.AddInValue("class", ' ', cssClass(), false);
            return attributes;
        }

        public static AttributeDictionary PrependCssClass(this AttributeDictionary attributes, Func<string> cssClass)
        {
            attributes.AddInValue("class", ' ', cssClass(), true);
            return attributes;
        }

        public static AttributeDictionary AppendCssClass(this AttributeDictionary attributes, string cssClass)
        {
            attributes.AddInValue("class", ' ', cssClass, false);
            return attributes;
        }

        public static AttributeDictionary PrependCssClass(this AttributeDictionary attributes, string cssClass)
        {
            attributes.AddInValue("class", ' ', cssClass, true);
            return attributes;
        }
    }
}
