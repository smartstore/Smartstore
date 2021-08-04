using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("input", Attributes = "[type=number]")]
    public class NumberInputTagHelper : TagHelper
    {
        const string PostfixAttributeName = "sm-postfix";
        const string DecimalsAttributeName = "sm-decimals";

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

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.AppendCssClass("numberinput");

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
                if (output.Attributes.TryGetAttribute("value", out var valueAttr) && valueAttr.Value != null)
                {
                    if (decimal.TryParse(valueAttr.Value.ToString(), out var value))
                    {
                        output.MergeAttribute("value", value.ToStringInvariant(), true);
                    }
                }
            }
            
            // Display label
            output.PostElement.AppendHtml("<span class='numberinput-formatted'></span>");

            // Postfix
            if (Postfix.HasValue())
            {
                output.PostElement.AppendHtml($"<span class='numberinput-postfix'>{Postfix}</span>");
            }

            // Step up
            output.PostElement.AppendHtml("<a href='javascript:;' class='numberinput-stepper numberinput-up' tabindex='-1'><i class='fa fa-chevron-up'></i></a>");

            // Step down
            output.PostElement.AppendHtml("<a href='javascript:;' class='numberinput-stepper numberinput-down' tabindex='-1'><i class='fa fa-chevron-down'></i></a>");

            // Parent wrapper tag
            var group = new TagBuilder("div");
            group.Attributes.Add("class", "numberinput-group input-group");

            if (context.AllAttributes.TryGetAttribute("sm-control-size", out var controlSizeAttr))
            {
                var controlSize = controlSizeAttr.Value.Convert<ControlSize?>();
                if (controlSize.HasValue)
                {
                    group.AppendCssClass("input-group-" + (controlSize == ControlSize.Small ? "sm" : "lg"));
                }

                context.Items.Add("IsNumberInput", true);
            }

            output.WrapElementWith(group);
        }
    }
}
