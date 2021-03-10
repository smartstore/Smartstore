using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("*", Attributes = AttributesName)]
    [HtmlTargetElement("*", Attributes = ConditionalAttributePrefix + "*")]
    public class AttributesTagHelper : TagHelper
    {
        const string AttributesName = "sm-attrs";
        const string ConditionalAttributePrefix = "sm-attr-";

        private IDictionary<string, (bool Condition, string Value)> _conditionalAttributes;

        public override int Order => 100;

        /// <summary>
        /// An <see cref="AttributeDictionary"/> instance whose content should be merged with local attributes.
        /// </summary>
        [HtmlAttributeName(AttributesName)]
        public AttributeDictionary Attributes { get; set; }

        /// <summary>
        /// Additional conditional attributes.
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
                    output.MergeAttribute(attr.Key, attr.Value, false);
                }
            }

            if (_conditionalAttributes != null && _conditionalAttributes.Count > 0)
            {
                foreach (var kvp in _conditionalAttributes)
                {
                    if (kvp.Value.Condition)
                    {
                        output.MergeAttribute(kvp.Key, kvp.Value.Value, false);
                    }
                }
            }
        }
    }
}