using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.UI.TagHelpers
{
    public static class TagHelperAttributeListExtensions
    {
        /// <summary>
        /// Adds a <see cref="TagHelperAttribute"/> with <paramref name="name"/> and <paramref name="value"/> to the end of the collection,
        /// but only if the attribute does not exist.
        /// </summary>
        /// <param name="name">
        /// The <see cref="TagHelperAttribute.Name"/> of the <see cref="TagHelperAttribute"/> to set.
        /// </param>
        /// <param name="value">
        /// The <see cref="TagHelperAttribute.Value"/> to set.
        /// </param>
        /// <remarks><paramref name="name"/> is compared case-insensitively.</remarks>
        public static void SetAttributeNoReplace(this TagHelperAttributeList attributes, string name, object value)
        {
            Guard.NotEmpty(name, nameof(name));
            
            if (!attributes.ContainsName(name))
            {
                attributes.SetAttribute(name, value);
            }
        }

        /// <summary>
        /// Adds a <see cref="TagHelperAttribute"/> with <paramref name="name"/> and the value of <paramref name="valueAccessor"/> to the end of the collection,
        /// but only if the attribute does not exist.
        /// </summary>
        /// <param name="name">
        /// The <see cref="TagHelperAttribute.Name"/> of the <see cref="TagHelperAttribute"/> to set.
        /// </param>
        /// <param name="valueAccessor">
        /// The <see cref="TagHelperAttribute.Value"/> to set.
        /// </param>
        /// <remarks><paramref name="name"/> is compared case-insensitively.</remarks>
        public static void SetAttributeNoReplace(this TagHelperAttributeList attributes, string name, Func<object> valueAccessor)
        {
            Guard.NotEmpty(name, nameof(name));

            if (!attributes.ContainsName(name))
            {
                attributes.SetAttribute(name, valueAccessor());
            }
        }
    }
}