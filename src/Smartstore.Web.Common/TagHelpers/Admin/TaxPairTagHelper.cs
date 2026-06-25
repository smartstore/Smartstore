using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Common.Services;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Admin;

/// <summary>
/// Represents tax pair kinds.
/// </summary>
public enum TaxPairKind
{
    UnitPrice,
    Discount,
    Shipping,
    PaymentFee,
    LineTotal,
    Subtotal,
    SubtotalDiscount
}

[Flags]
public enum TaxPairMode
{
    /// <summary>
    /// Gross and net values are calculated.
    /// </summary>
    Calculate = 1 << 0,

    /// <summary>
    /// Gross and net values are edited manually.
    /// </summary>
    Edit = 1 << 1
}

public enum TaxPairAlignment
{
    Horizontal, 
    Vertical
}

[HtmlTargetElement(TagName, TagStructure = TagStructure.NormalOrSelfClosing)]
public class TaxPairTagHelper : SmartTagHelper
{
    const string TagName = "tax-pair";
    const string KindAttributeName = "kind";
    const string ModeAttributeName = "mode";
    const string ForGrossAttributeName = "asp-for-gross";
    const string ForNetAttributeName = "asp-for-net";
    const string AlignAttributeName = "align";

    private readonly ICurrencyService _currencyService;

    public TaxPairTagHelper(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    /// <summary>
    /// Gets or sets the kind of the tax pair.
    /// It is required to find other tax pairs in order to automatically calculate their values.
    /// </summary>
    [HtmlAttributeName(KindAttributeName)]
    public TaxPairKind? Kind { get; set; }

    /// <summary>
    /// Gets or sets the mode of the tax pair which determines whether the gross and net values are calculated, edited manually or both.
    /// </summary>
    [HtmlAttributeName(ModeAttributeName)]
    public TaxPairMode Mode { get; set; } = TaxPairMode.Calculate | TaxPairMode.Edit;

    /// <summary>
    /// An expression for the gross property of the view model.
    /// </summary>
    [HtmlAttributeName(ForGrossAttributeName)]
    public ModelExpression ForGross { get; set; }

    /// <summary>
    /// An expression for the net property of the view model.
    /// </summary>
    [HtmlAttributeName(ForNetAttributeName)]
    public ModelExpression ForNet { get; set; }

    /// <summary>
    /// Gets or sets the alignment of the tax pair editors.
    /// </summary>
    [HtmlAttributeName(AlignAttributeName)]
    public TaxPairAlignment Align { get; set; } = TaxPairAlignment.Horizontal;

    protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
    {
        var primaryCurrencyCode = _currencyService.PrimaryCurrency.CurrencyCode;
        var calculate = Mode.HasFlag(TaxPairMode.Calculate);
        var active = calculate;

        if (calculate)
        {
            var grossValue = ForGross.Model.Convert<decimal?>() ?? 0;
            var netValue = ForNet.Model.Convert<decimal?>() ?? 0;

            // Deactivate conversion by default if the net and gross amounts are the same.
            active = (grossValue == 0 && netValue == 0) || grossValue > netValue;
        }

        // Main div container.
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;

        var isVertical = Align == TaxPairAlignment.Vertical;
        var containerClass = isVertical ? "d-flex align-items-center" : "row g-1 flex-nowrap";
        var inputsWrapperClass = isVertical ? "d-flex flex-column gap-1 flex-grow-1" : "row g-1 flex-nowrap";

        // Clear all existing attributes. We apply them on the editors.
        output.Attributes.Clear();
        output.Attributes.Add("class", containerClass);
        output.Attributes.Add("data-tax-pair", Kind == null ? string.Empty : Kind.ToString().ToLower());

        if (active)
        {
            output.Attributes.Add("data-tax-active", string.Empty);
        }

        output.Content.Clear();

        // Lock/unlock button
        var lockDiv = new TagBuilder("div");
        lockDiv.AddCssClass(isVertical ? "ms-2" : "col-auto align-content-center");
        lockDiv.InnerHtml.AppendHtml(CreateLockTag());

        var editorGross = BuildEditor(ForGross, true);
        var editorNet = BuildEditor(ForNet, false);

        if (isVertical)
        {
            // Wrap editors in a column for vertical layout
            var inputsWrapper = new TagBuilder("div");
            inputsWrapper.AddCssClass(inputsWrapperClass);
            inputsWrapper.InnerHtml.AppendHtml(editorGross);
            inputsWrapper.InnerHtml.AppendHtml(editorNet);

            output.Content.AppendHtml(inputsWrapper);
            output.Content.AppendHtml(lockDiv);
        }
        else
        {
            // Standard horizontal flow
            output.Content.AppendHtml(editorGross);
            output.Content.AppendHtml(lockDiv);
            output.Content.AppendHtml(editorNet);
        }

        TagBuilder CreateLockTag()
        {
            TagBuilder lockTag;
            if (calculate)
            {
                lockTag = new TagBuilder("button");
                lockTag.Attributes.Add("type", "button");
                lockTag.AddCssClass("btn btn-sm border-0 shadow-none bg-transparent text-reset p-1 btn-tax-lock");
                lockTag.Attributes.Add("title", T(active ? "Admin.Common.TaxCalculator.Disable" : "Admin.Common.TaxCalculator.Enable"));
                lockTag.InnerHtml.AppendHtml("<i class='fa {0}'></i>".FormatInvariant(active ? "fa-lock" : "fa-lock-open text-muted"));
            }
            else
            {
                // Automatic gross-to-net conversion is not possible.
                lockTag = new TagBuilder("span");
                lockTag.AddCssClass("p-1");
                lockTag.Attributes.Add("title", T("Admin.Common.TaxCalculator.NoCalculation"));
                lockTag.InnerHtml.AppendHtml("<i class='fa fa-lock-open text-danger'></i>");
            }

            return lockTag;
        }

        TagBuilder BuildEditor(ModelExpression expression, bool gross)
        {
            var postfix = T("Admin.Orders.Fields.Edit." + (gross ? "InclTax" : "ExclTax"), primaryCurrencyCode).ToString();
            var baseId = context.AllAttributes["data-id"]?.Value?.ToString();
            var cssClass = context.AllAttributes["class"]?.Value?.ToString();

            var htmlAttributes = new Dictionary<string, object>
            {
                ["data-tax-field"] = gross ? "gross" : "net"
            };

            if (cssClass.HasValue())
            {
                htmlAttributes.Add("class", cssClass);
            }
            if (baseId.HasValue())
            {
                htmlAttributes.Add("data-id", baseId.FormatInvariant(gross ? "incl" : "excl"));
            }

            var editor = HtmlHelper.EditorFor(expression, new RouteValueDictionary
            {
                ["postfix"] = postfix,
                ["htmlAttributes"] = htmlAttributes
            });

            var div = new TagBuilder("div");
            div.AddCssClass(isVertical ? string.Empty : "col");
            div.InnerHtml.AppendHtml(editor);

            return div;
        }
    }
}
