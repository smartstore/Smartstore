using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Admin;

/// <summary>
/// Edits a numeric amount and a separate Boolean indicating whether it represents a percentage.
/// </summary>
[HtmlTargetElement("amount-or-percent", Attributes = "asp-for,percentage-for", TagStructure = TagStructure.NormalOrSelfClosing)]
public class AmountOrPercentTagHelper : BaseFormTagHelper
{
    private IDictionary<string, string> _valueAttributes;
    private IDictionary<string, string> _percentageAttributes;

    /// <summary>
    /// Gets or sets the Boolean property selecting percentage mode.
    /// </summary>
    [HtmlAttributeName("percentage-for")]
    public ModelExpression PercentageFor { get; set; }

    /// <summary>
    /// Gets or sets the unit displayed for absolute values. Defaults to a localized absolute-mode label.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// Additional attributes for the numeric input, without the value- prefix.
    /// </summary>
    [HtmlAttributeName("value-attributes", DictionaryAttributePrefix = "value-")]
    public IDictionary<string, string> ValueAttributes
    {
        get
        {
            return _valueAttributes ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        set
        {
            _valueAttributes = value;
        }
    }

    /// <summary>
    /// Additional attributes for both Boolean radios, without the percentage- prefix.
    /// </summary>
    [HtmlAttributeName("percentage-attributes", DictionaryAttributePrefix = "percentage-")]
    public IDictionary<string, string> PercentageAttributes
    {
        get
        {
            return _percentageAttributes ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        set
        {
            _percentageAttributes = value;
        }
    }

    protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
    {
        Guard.IsTrue(PercentageFor.ModelExplorer.ModelType == typeof(bool), nameof(PercentageFor),
            "PercentageFor must reference a Boolean property.");

        string unit = Unit.NullEmpty() ?? T("Common.AmountOrPercent.Absolute");
        var htmlAttributes = ValueAttributes.ToDictionary(x => x.Key, x => (object)x.Value, StringComparer.OrdinalIgnoreCase);

        var input = HtmlHelper.EditorFor(For, new RouteValueDictionary
        {
            ["htmlAttributes"] = htmlAttributes
        });

        var modes = new TagBuilder("div");
        modes.AddCssClass("input-group-prepend number-input-prepend btn-group-toggle flex-nowrap");
        modes.Attributes["data-toggle"] = "buttons";

        foreach (bool percentage in new[] { true, false })
        {
            var value = percentage ? "true" : "false";
            var radio = HtmlGenerator.GenerateRadioButton(ViewContext, PercentageFor.ModelExplorer, PercentageFor.Name, value, null, new { autocomplete = "off" });
            radio.Attributes["id"] += "_" + value;
            radio.Attributes["data-amount-percentage"] = string.Empty;
            radio.Attributes["data-value-type"] = "boolean";

            radio.MergeAttributes(PercentageAttributes, replaceExisting: true);

            var label = new TagBuilder("label");
            label.AddCssClass("btn btn-light btn-icon mb-0");

            if (radio.Attributes.ContainsKey("checked"))
            {
                label.AddCssClass("active");
            }

            label.Attributes["for"] = radio.Attributes["id"];
            label.Attributes["title"] = T(percentage ? "Common.AmountOrPercent.Percentage" : "Common.AmountOrPercent.Absolute");
            label.InnerHtml.AppendHtml(radio);
            label.InnerHtml.Append(percentage ? "%" : unit);
            modes.InnerHtml.AppendHtml(label);
        }

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AppendCssClass("input-group flex-nowrap");
        output.Attributes.SetAttribute("data-amount-or-percent", string.Empty);
        output.Content.AppendHtml(modes);
        output.Content.AppendHtml(input);
    }
}
