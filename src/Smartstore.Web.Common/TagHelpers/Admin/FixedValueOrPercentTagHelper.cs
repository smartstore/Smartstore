using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Admin;

/// <summary>
/// Edits a numeric amount and a separate Boolean indicating whether it represents a percentage.
/// </summary>
[HtmlTargetElement(TagName, Attributes = ForAttributeName + "," + ForPercentageAttributeName, TagStructure = TagStructure.NormalOrSelfClosing)]
public class FixedValueOrPercentTagHelper : BaseFormTagHelper
{
    const string TagName = "fixed-or-percent";
    const string ForPercentageAttributeName = "asp-for-percentage";
    const string PostfixAttributeName = "sm-postfix";
    const string ValueAttributesAttributeName = "value-attributes";
    const string ValueAttributePrefix = "value-";
    const string PercentageAttributesAttributeName = "percentage-attributes";
    const string PercentageAttributePrefix = "percentage-";

    private IDictionary<string, string> _valueAttributes;
    private IDictionary<string, string> _percentageAttributes;

    /// <summary>
    /// Gets or sets the Boolean property selecting percentage mode.
    /// </summary>
    [HtmlAttributeName(ForPercentageAttributeName)]
    public ModelExpression ForPercentage { get; set; }

    /// <summary>
    /// Gets or sets the postfix displayed for fixed values. Defaults to a localized fixed-value label.
    /// Percentage mode always displays the percent sign.
    /// </summary>
    [HtmlAttributeName(PostfixAttributeName)]
    public string Postfix { get; set; }

    /// <summary>
    /// Additional attributes for the numeric input, without the value- prefix.
    /// </summary>
    [HtmlAttributeName(ValueAttributesAttributeName, DictionaryAttributePrefix = ValueAttributePrefix)]
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
    /// Additional attributes for the Boolean checkbox, without the percentage- prefix.
    /// </summary>
    [HtmlAttributeName(PercentageAttributesAttributeName, DictionaryAttributePrefix = PercentageAttributePrefix)]
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
        Guard.IsTrue(ForPercentage.ModelExplorer.ModelType == typeof(bool), nameof(ForPercentage),
            "ForPercentage must reference a Boolean property.");

        string postfix = Postfix.NullEmpty() ?? T("FixedValueOrPercent.Absolute");
        var htmlAttributes = ValueAttributes.ToDictionary(x => x.Key, x => (object)x.Value, StringComparer.OrdinalIgnoreCase);

        var checkbox = HtmlGenerator.GenerateCheckBox(ViewContext, ForPercentage.ModelExplorer, ForPercentage.Name, null, new { autocomplete = "off" });
        checkbox.Attributes["data-fixed-or-percent-percentage"] = string.Empty;
        checkbox.Attributes["data-value-type"] = "boolean";
        checkbox.Attributes["aria-label"] = T("FixedValueOrPercent.Percentage");
        checkbox.MergeAttributes(PercentageAttributes, replaceExisting: true);
        checkbox.AddCssClass("d-none");
        checkbox.Attributes["tabindex"] = "-1";

        bool isPercentage = checkbox.Attributes.ContainsKey("checked");
        var input = HtmlHelper.EditorFor(For, new RouteValueDictionary
        {
            ["htmlAttributes"] = htmlAttributes,
            ["postfix"] = System.Net.WebUtility.HtmlEncode(isPercentage ? "%" : postfix)
        });

        var modes = new TagBuilder("div");
        modes.AddCssClass("input-group-prepend input-addon-plain");

        var addon = new TagBuilder("div");
        addon.AddCssClass("input-group-text px-1 py-0");

        var button = new TagBuilder("button");
        button.AddCssClass("btn btn-xs btn-plain btn-flat btn-icon no-chevron");
        button.Attributes["type"] = "button";
        button.Attributes["data-fixed-or-percent-toggle"] = string.Empty;
        button.Attributes["aria-pressed"] = isPercentage ? "true" : "false";
        button.Attributes["aria-label"] = T("FixedValueOrPercent.Percentage");
        button.Attributes["title"] = T("FixedValueOrPercent.Percentage");
        if (isPercentage)
        {
            button.AddCssClass("active");
        }

        if (checkbox.Attributes.ContainsKey("disabled"))
        {
            button.Attributes["disabled"] = "disabled";
        }

        button.InnerHtml.Append("%");
        addon.InnerHtml.AppendHtml(button);
        modes.InnerHtml.AppendHtml(addon);

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AppendCssClass("input-group flex-nowrap");
        output.Attributes.SetAttribute("data-fixed-or-percent", string.Empty);
        output.Attributes.SetAttribute("data-fixed-or-percent-postfix", postfix);
        output.Content.AppendHtml(modes);
        output.Content.AppendHtml(input);
        output.Content.AppendHtml(checkbox);
        output.Content.AppendHtml(HtmlGenerator.GenerateHiddenForCheckbox(ViewContext, ForPercentage.ModelExplorer, ForPercentage.Name));
    }
}
