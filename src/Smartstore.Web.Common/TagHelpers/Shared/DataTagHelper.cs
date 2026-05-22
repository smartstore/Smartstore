using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;

namespace Smartstore.Web.TagHelpers.Shared;

/// <summary>
/// Outputs a &lt;data&gt; tag for a <see cref="Money"/> value, 
/// formatted and rounded according to the currency's properties.
/// </summary>
[HtmlTargetElement("data", Attributes = ForAttributeName)]
public class DataTagHelper(IRoundingHelper roundingHelper) : BaseFormTagHelper
{
    private readonly IRoundingHelper _roundingHelper = roundingHelper;

    protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
    {
        var value = For?.Model;

        if (value is not Money money)
        {
            output.SuppressOutput();
            return;
        }

        output.Attributes.SetAttribute("value", _roundingHelper.Round(money).ToStringInvariant());
        output.Attributes.SetAttribute("data-currency", money.Currency?.CurrencyCode);
        output.AppendCssClass("text-nowrap");

        var childContent = await output.GetChildContentAsync();
        if (childContent.IsEmptyOrWhiteSpace)
        {
            output.Content.SetHtmlContent(money.ToString());
        }
    }
}