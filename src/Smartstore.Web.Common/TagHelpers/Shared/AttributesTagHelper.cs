using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("*", Attributes = AttributesName)]
    [HtmlTargetElement("*", Attributes = ConditionalAttributePrefix + "*")]
    public class AttributesTagHelper : TagHelper
    {
        const string AttributesName = "attrs";
        const string ConditionalAttributePrefix = "attr-";

        private IDictionary<string, (bool Condition, string Value)> _conditionalAttributes;

        /// <summary>
        /// We set the order to -1100 to run this TagHelper before <see cref="Microsoft.AspNetCore.Mvc.TagHelpers.InputTagHelper"/>.
        /// We had to do this because of the QtyInput EditorTemplate which is rendered multiple times on the cart page and needs a unique id and name
        /// which will be passed to the EditorTemplate by htmlAttributes dictionary and will be passed to the AttributesTagHelper.
        /// <see cref="Microsoft.AspNetCore.Mvc.TagHelpers.InputTagHelper"/> does not override the id and name attributes if they are already set.
        /// </summary>
        public override int Order => -1100;

        /// <summary>
        /// An <see cref="AttributeDictionary"/> instance whose content should be merged with local attributes.
        /// </summary>
        [HtmlAttributeName(AttributesName)]
        public AttributeDictionary Attributes { get; set; }

        /// <summary>
        /// Conditional attribute. Output will be suppressed if condition (first value tuple item) evaluates to <c>false</c>.
        /// NOTE: the value of conditional <c>class</c> attribute (sm-class) will be appended to existing <c>class</c> attribute.
        /// </summary>
        [HtmlAttributeName("sm-all-conditional-attrs", DictionaryAttributePrefix = ConditionalAttributePrefix)]
        public IDictionary<string, (bool Condition, string Value)> ConditionalAttributes
        {
            get
            {
                return _conditionalAttributes ??= new Dictionary<string, (bool, string)>(StringComparer.OrdinalIgnoreCase);
            }
            set
            {
                _conditionalAttributes = value;
            }
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (Attributes != null)
            {
                foreach (var attr in Attributes)
                {
                    if (attr.Key == "class")
                    {
                        output.AppendCssClass(attr.Value);
                    }
                    else
                    {
                        output.MergeAttribute(attr.Key, attr.Value, attr.Key == "value");
                    }
                }
            }

            if (_conditionalAttributes != null && _conditionalAttributes.Count > 0)
            {
                foreach (var kvp in _conditionalAttributes)
                {
                    if (kvp.Value.Condition)
                    {
                        if (kvp.Key == "class")
                        {
                            output.AppendCssClass(kvp.Value.Value);
                        }
                        else
                        {
                            output.MergeAttribute(kvp.Key, kvp.Value.Value, kvp.Key == "value");
                        }
                    }
                }
            }
        }
    }
}