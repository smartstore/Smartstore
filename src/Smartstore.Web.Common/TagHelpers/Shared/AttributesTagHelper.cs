using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("*", Attributes = AttributesName)]
    public class AttributesTagHelper : TagHelper
    {
        const string AttributesName = "attributes";

        /// <summary>
        /// A <see cref="AttributeDictionary"/> instance.
        /// </summary>
        [HtmlAttributeName(AttributesName)]
        public AttributeDictionary Attributes { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            foreach (var attr in Attributes)
            {
                output.MergeAttribute(attr.Key, attr.Value, false);
            }
        }
    }
}