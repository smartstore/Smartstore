using Autofac;
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
    SubTotal,
    LineTotal,
    UnitPrice
}

public enum TaxPairState
{
    /// <summary>
    /// Gross and net values are calculated by default.
    /// The user may switch to manual editing. This is the default state.
    /// </summary>
    CalculateOrEdit,

    /// <summary>
    /// Gross and net values are always calculated.
    /// The user cannot switch to manual editing.
    /// </summary>
    /// <remarks>Not supported yet. For future use.</remarks>
    CalculateOnly,

    /// <summary>
    /// Gross and net values are edited manually by default.
    /// The user may switch to calculated values.
    /// </summary>
    /// <remarks>Not supported yet. For future use.</remarks>
    EditOrCalculate,

    /// <summary>
    /// Gross and net values are always edited manually.
    /// The user cannot switch to calculated values.
    /// </summary>
    EditOnly
}

[HtmlTargetElement(TagName, TagStructure = TagStructure.NormalOrSelfClosing)]
public class TaxPairTagHelper : SmartTagHelper
{
    const string TagName = "tax-pair";
    const string KindAttributeName = "kind";
    const string StateAttributeName = "state";
    const string ForGrossAttributeName = "asp-for-gross";
    const string ForNetAttributeName = "asp-for-net";

    private ICurrencyService _currencyService;

    /// <summary>
    /// Gets or sets the kind of the tax pair.
    /// It is required to find other tax pairs in order to automatically calculate their values.
    /// </summary>
    [HtmlAttributeName(KindAttributeName)]
    public TaxPairKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the state of the tax pair which determines whether the gross and net values are calculated or edited manually.
    /// </summary>
    [HtmlAttributeName(StateAttributeName)]
    public TaxPairState State { get; set; } = TaxPairState.CalculateOrEdit;

    /// <summary>
    /// An expression for the gross property of the view model.
    /// </summary>
    [HtmlAttributeName(ForGrossAttributeName)]
    public ModelExpression GrossProperty { get; set; }

    /// <summary>
    /// An expression for the net property of the view model.
    /// </summary>
    [HtmlAttributeName(ForNetAttributeName)]
    public ModelExpression NetProperty { get; set; }

    [HtmlAttributeNotBound]
    protected internal ICurrencyService CurrencyService
    {
        get => _currencyService ??= ViewContext.HttpContext.GetServiceScope().Resolve<ICurrencyService>();
    }

    protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
    {
        ProcessCoreAsync(context, output).Await();
    }

    protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
    {
        var primaryCurrencyCode = CurrencyService.PrimaryCurrency.CurrencyCode;

        // Main div container.
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;

        output.Attributes.Add("class", "row g-1 flex-nowrap");
        output.Attributes.Add("data-tax-pair", Kind.ToString().ToLower());

        if (State == TaxPairState.CalculateOrEdit || State == TaxPairState.CalculateOnly)
        {
            output.Attributes.Add("data-tax-active", string.Empty);
        }

        output.Content.Clear();

        // Editor for gross.
        var grossEditor = HtmlHelper.EditorFor(GrossProperty, new RouteValueDictionary
        {
            ["postfix"] = T("Admin.Orders.Fields.Edit.InclTax", primaryCurrencyCode),
            ["htmlAttributes"] = new Dictionary<string, object>
            {
                ["data-tax-field"] = "gross"
            }
        });

        var grossDiv = new TagBuilder("div");
        grossDiv.AddCssClass("col");
        grossDiv.InnerHtml.AppendHtml(grossEditor);

        // Lock/unlock button.
        TagBuilder lockTag;
        if (State == TaxPairState.EditOnly)
        {
            // Automatic gross-to-net conversion is not possible.
            lockTag = new TagBuilder("span");
            lockTag.AddCssClass("p-1");
            lockTag.Attributes.Add("title", T("Admin.Common.TaxCalculator.NoCalculation"));
            lockTag.InnerHtml.AppendHtml("<i class='fa fa-lock-open text-danger'></i>");
        }
        else
        {
            lockTag = new TagBuilder("button");
            lockTag.Attributes.Add("type", "button");
            lockTag.AddCssClass("btn btn-sm border-0 shadow-none bg-transparent text-reset p-1 btn-tax-lock");
            lockTag.Attributes.Add("title", T("Admin.Common.TaxCalculator.Disable"));
            lockTag.InnerHtml.AppendHtml("<i class='fa fa-lock'></i>");
        }

        var lockDiv = new TagBuilder("div");
        lockDiv.AddCssClass("col-auto align-content-center");
        lockDiv.InnerHtml.AppendHtml(lockTag);

        // Editor for net.
        var netEditor = HtmlHelper.EditorFor(NetProperty, new RouteValueDictionary
        {
            ["postfix"] = T("Admin.Orders.Fields.Edit.ExclTax", primaryCurrencyCode),
            ["htmlAttributes"] = new Dictionary<string, object>
            {
                ["data-tax-field"] = "net"
            }
        });

        var netDiv = new TagBuilder("div");
        netDiv.AddCssClass("col");
        netDiv.InnerHtml.AppendHtml(netEditor);

        // Put all together.
        output.Content.AppendHtml(grossDiv);
        output.Content.AppendHtml(lockDiv);
        output.Content.AppendHtml(netDiv);
    }
}
