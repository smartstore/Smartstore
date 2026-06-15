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
    UnitPrice,
    Total
}

[HtmlTargetElement(TagName, TagStructure = TagStructure.NormalOrSelfClosing)]
public class TaxPairTagHelper : SmartTagHelper
{
    const string TagName = "tax-pair";
    const string KindAttributeName = "kind";
    const string ForGrossAttributeName = "asp-for-gross";
    const string ForNetAttributeName = "asp-for-net";

    private ICurrencyService _currencyService;

    /// <summary>
    /// Gets or sets the kind of the tax pair.
    /// </summary>
    [HtmlAttributeName(KindAttributeName)]
    public TaxPairKind Kind { get; set; }

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
        output.Attributes.Add("data-tax-active", string.Empty);

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

        var divGross = new TagBuilder("div");
        divGross.AddCssClass("col");
        divGross.InnerHtml.AppendHtml(grossEditor);

        // Lock/unlock button.
        var btnTag = new TagBuilder("button");
        btnTag.Attributes.Add("type", "button");
        btnTag.Attributes.Add("title", T("Admin.Common.TaxCalculator.Disable"));
        btnTag.AddCssClass("btn btn-sm border-0 shadow-none bg-transparent text-reset p-1 btn-tax-lock");
        btnTag.InnerHtml.AppendHtml("<i class='fa fa-lock'></i>");

        var btnDiv = new TagBuilder("div");
        btnDiv.AddCssClass("col-auto align-content-center");
        btnDiv.InnerHtml.AppendHtml(btnTag);

        // Editor for net.
        var netEditor = HtmlHelper.EditorFor(NetProperty, new RouteValueDictionary
        {
            ["postfix"] = T("Admin.Orders.Fields.Edit.ExclTax", primaryCurrencyCode),
            ["htmlAttributes"] = new Dictionary<string, object>
            {
                ["data-tax-field"] = "net"
            }
        });

        var divNet = new TagBuilder("div");
        divNet.AddCssClass("col");
        divNet.InnerHtml.AppendHtml(netEditor);

        // Put all together.
        output.Content.AppendHtml(divGross);
        output.Content.AppendHtml(btnDiv);
        output.Content.AppendHtml(divNet);
    }
}
