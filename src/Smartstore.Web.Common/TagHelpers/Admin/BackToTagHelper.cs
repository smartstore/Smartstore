using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.IconKit;

using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Admin;

[HtmlTargetElement("a", Attributes = BackToAttributeName)]
public class BackToTagHelper : SmartTagHelper
{
    protected override string GenerateTagId(TagHelperContext context) => null;

    const string BackToAttributeName = "sm-backto";

    /// <summary>
    /// Renders the link as a regular button with a left arrow icon.
    /// </summary>
    [HtmlAttributeName(BackToAttributeName)]
    public bool IsBackButton { get; set; }

    protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (!IsBackButton)
        {
            return;
        }

        output.AppendCssClass("btn btn-outline-secondary btn-sm btn-icon");

        if (!output.Attributes.ContainsName("title"))
        {
            output.Attributes.SetAttribute("title", T("Admin.Common.BackToList"));
        }

        var icon = await HtmlHelper.IconAsync("arrow-left", options: new IconOptions { Size = "lg" });
        output.Content.SetHtmlContent(icon);
    }
}