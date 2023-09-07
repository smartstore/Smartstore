using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    public enum NumberInputStyle
    {
        /// <summary>
        /// Up/Down chevrons on right, number left-aligned, postfix right-aligned.
        /// </summary>
        Default,

        /// <summary>
        /// Minus on left, Plus on right, number centered, postfix centered below number.
        /// </summary>
        Centered
    }

    [HtmlTargetElement("input", Attributes = "[type=number]")]
    public class NumberInputTagHelper : TagHelper
    {
        const string PostfixAttributeName = "sm-postfix";
        const string DecimalsAttributeName = "sm-decimals";
        const string StyleAttributeName = "sm-numberinput-style";
        const string GroupClassAttributeName = "sm-numberinput-group-class";

        private readonly ILocalizationService _localizationService;

        public NumberInputTagHelper(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        // Must come before FormControlTagHelper because of sizing issues.
        public override int Order => 0;

        /// <summary>
        /// The text which will be displayed inside the number input as a post fix.
        /// </summary>
        [HtmlAttributeName(PostfixAttributeName)]
        public string Postfix { get; set; }

        /// <summary>
        /// Number of decimal digits.
        /// </summary>
        [HtmlAttributeName(DecimalsAttributeName)]
        public uint Decimals { get; set; }

        /// <summary>
        /// Style of numberinput control.
        /// </summary>
        [HtmlAttributeName(StyleAttributeName)]
        public NumberInputStyle Style { get; set; }

        /// <summary>
        /// CSS class of parent group tag.
        /// </summary>
        [HtmlAttributeName(GroupClassAttributeName)]
        public string GroupClass { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.AppendCssClass("numberinput");
            output.MergeAttribute("data-editor", "numberinput");

            if (Decimals > 0)
            {
                output.MergeAttribute("data-decimals", Decimals.ToStringInvariant());
            }

            var modelExpression = context.AllAttributes["asp-for"]?.Value as ModelExpression;
            if (modelExpression != null)
            {
                // Placeholder
                if (modelExpression.Metadata.IsNullableValueType && !output.Attributes.ContainsName("placeholder"))
                {
                    output.Attributes.Add("placeholder", _localizationService.GetResource("Common.Empty", logIfNotFound: false, returnEmptyIfNotFound: true));
                }

                // Fix value decimal separator (make invariant)
                var value = modelExpression.Model.Convert<decimal?>();
                if (value.HasValue)
                {
                    output.MergeAttribute("value", value.ToStringInvariant(), true);
                }
            }

            // Display label
            output.PostElement.AppendHtml("<span class='numberinput-formatted'></span>");

            // Postfix
            if (Postfix.HasValue())
            {
                output.PostElement.AppendHtml($"<span class='numberinput-postfix'>{Postfix}</span>");
            }

            // Step down
            var stepDownIcon = "fas fa-" + (Style == NumberInputStyle.Centered ? "minus" : "chevron-down");
            output.PostElement.AppendHtml($"<a href='javascript:;' class='numberinput-stepper numberinput-down' tabindex='-1'><i class='{stepDownIcon}'></i></a>");

            // Step up
            var stepUpIcon = "fas fa-" + (Style == NumberInputStyle.Centered ? "plus" : "chevron-up");
            output.PostElement.AppendHtml($"<a href='javascript:;' class='numberinput-stepper numberinput-up' tabindex='-1'><i class='{stepUpIcon}'></i></a>");

            // Parent wrapper tag
            var group = new TagBuilder("div");
            group.Attributes.Add("class", "numberinput-group input-group edit-control");
            group.MergeAttribute("data-editor", "number");

            group.AppendCssClass("numberinput-" + (Style == NumberInputStyle.Centered ? "centered" : "default"));

            if (GroupClass.HasValue())
            {
                group.AppendCssClass(GroupClass);
            }

            if (Postfix.HasValue())
            {
                group.AppendCssClass("has-postfix");
            }

            if (context.AllAttributes.TryGetAttribute("sm-control-size", out var controlSizeAttr))
            {
                var controlSize = controlSizeAttr.Value.Convert<ControlSize?>();
                if (controlSize.HasValue && controlSize != ControlSize.Medium)
                {
                    group.AppendCssClass("input-group-" + (controlSize == ControlSize.Small ? "sm" : "lg"));
                }

                context.Items.Add("IsNumberInput", true);
            }
            else if (ViewContext.ViewData.TryGetValue("size", out var size) && size is string sizeStr)
            {
                group.AppendCssClass($"input-group-{sizeStr}");
            }

            output.WrapElementWith(group);
        }
    }
}
