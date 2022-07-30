using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers
{
    public static class TagHelperAttributeListExtensions
    {
        /// <summary>
        /// Copies all attributes from <paramref name="attributes"/> to <paramref name="target"/>
        /// overriding any existing attribute.
        /// </summary>
        public static void CopyTo(this TagHelperAttributeList attributes, AttributeDictionary target)
        {
            Guard.NotNull(attributes, nameof(attributes));
            Guard.NotNull(target, nameof(target));

            foreach (var attr in attributes)
            {
                target[attr.Name] = attr.ValueAsString();
            }
        }

        /// <summary>
        /// Adds a <see cref="TagHelperAttribute"/> with <paramref name="name"/> and <paramref name="value"/> to the end of the collection,
        /// but only if the attribute does not exist.
        /// </summary>
        /// <param name="name">The <see cref="TagHelperAttribute.Name"/> of the <see cref="TagHelperAttribute"/> to set.</param>
        /// <param name="value">The <see cref="TagHelperAttribute.Value"/> to set.</param>
        /// <param name="ignoreNull"><c>false</c> = don't render attribute if value is null.</param>
        /// <remarks><paramref name="name"/> is compared case-insensitively.</remarks>
        public static void SetAttributeNoReplace(this TagHelperAttributeList attributes, string name, object value, bool ignoreNull = true)
        {
            Guard.NotEmpty(name, nameof(name));

            if (!attributes.ContainsName(name) && (value != null || !ignoreNull))
            {
                attributes.SetAttribute(name, value);
            }
        }

        /// <summary>
        /// Adds a <see cref="TagHelperAttribute"/> with <paramref name="name"/> and the value of <paramref name="valueAccessor"/> to the end of the collection,
        /// but only if the attribute does not exist.
        /// </summary>
        /// <param name="name">The <see cref="TagHelperAttribute.Name"/> of the <see cref="TagHelperAttribute"/> to set.</param>
        /// <param name="valueAccessor">The <see cref="TagHelperAttribute.Value"/> to set.</param>
        /// <param name="ignoreNull"><c>false</c> = don't render attribute if value is null.</param>
        /// <remarks><paramref name="name"/> is compared case-insensitively.</remarks>
        public static void SetAttributeNoReplace(this TagHelperAttributeList attributes, string name, Func<object> valueAccessor, bool ignoreNull = true)
        {
            Guard.NotEmpty(name, nameof(name));

            if (!attributes.ContainsName(name))
            {
                var value = valueAccessor();
                if (value != null || !ignoreNull)
                {
                    attributes.SetAttribute(name, valueAccessor());
                }
            }
        }

        /// <summary>
        /// Copies the html attribute <paramref name="name"/> from source <paramref name="attributes"/> to <see cref="TagBuilder"/> <paramref name="target"/>.
        /// </summary>
        /// <param name="attributes">The source to find attribute to copy.</param>
        /// <param name="target">The target <see cref="TagBuilder"/> instance to copy found attribute to.</param>
        /// <param name="name">Name of attribute to find in source.</param>
        /// <param name="replaceExisting">Whether to replace existing attribute in <paramref name="target"/>.</param>
        /// <param name="ignoreNull">Whether to skip copying if source attribute's value is <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if an attribute has been copied, <see langword="false"/> otherwise.</returns>
        public static bool CopyAttribute(this TagHelperAttributeList attributes, TagBuilder target, string name, bool replaceExisting = true, bool ignoreNull = true)
        {
            Guard.NotEmpty(name, nameof(name));
            Guard.NotNull(target, nameof(target));

            if (attributes.TryGetAttribute(name, out var attribute))
            {
                return target.MergeAttribute(name, () => attribute.ValueAsString(), replaceExisting, ignoreNull);
            }

            return false;
        }

        /// <summary>
        /// Moves the html attribute <paramref name="name"/> from source <paramref name="attributes"/> to <see cref="TagBuilder"/> <paramref name="target"/>.
        /// </summary>
        /// <param name="attributes">The source to find attribute to move.</param>
        /// <param name="target">The target <see cref="TagBuilder"/> instance to move found attribute to.</param>
        /// <param name="name">Name of attribute to find in source.</param>
        /// <param name="replaceExisting">Whether to replace existing attribute in <paramref name="target"/>.</param>
        /// <param name="ignoreNull">Whether to skip moving if source attribute's value is <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if an attribute has been moved, <see langword="false"/> otherwise.</returns>
        public static bool MoveAttribute(this TagHelperAttributeList attributes, TagBuilder target, string name, bool replaceExisting = true, bool ignoreNull = true)
        {
            Guard.NotEmpty(name, nameof(name));
            Guard.NotNull(target, nameof(target));

            if (attributes.TryGetAttribute(name, out var attribute))
            {
                if (target.MergeAttribute(name, () => attribute.ValueAsString(), replaceExisting, ignoreNull))
                {
                    attributes.RemoveAll(name);
                    return true;
                }
            }

            return false;
        }
    }
}