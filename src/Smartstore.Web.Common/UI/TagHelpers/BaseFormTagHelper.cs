using System;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.UI.TagHelpers
{
    public abstract class BaseFormTagHelper : SmartTagHelper
    {
        protected const string ForAttributeName = "asp-for";
        protected const string RequiredAttributeName = "asp-required";

        /// <summary>
        /// An expression to be evaluated against the current model.
        /// </summary>
        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; }

        [HtmlAttributeName(RequiredAttributeName)]
        public bool? IsRequired { get; set; }

        [HtmlAttributeNotBound]
        protected LocalizedDisplayNameAttribute LocalizedDisplayName { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            IsRequired ??= For?.Metadata.IsRequired;

            if (For != null
                && For.Metadata.AdditionalValues.TryGetValue(nameof(LocalizedDisplayNameAttribute), out var value)
                && value is LocalizedDisplayNameAttribute attribute)
            {
                LocalizedDisplayName = attribute;
            }
        }
    }
}
