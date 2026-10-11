using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Content.Media.Icons;
using Smartstore.Core.Localization;

namespace Smartstore.Web.TagHelpers.Admin;

[HtmlTargetElement("a", Attributes = BackToAttributeName)]
public class BackToTagHelper(Localizer localizer, IIconService icons, IIconRenderer iconRenderer) : TagHelper
{
    const string BackToAttributeName = "sm-backto";

    private Localizer T { get; } = localizer;

    /// <summary>
    /// Renders the link as a regular button with a left arrow icon.
    /// </summary>
    [HtmlAttributeName(BackToAttributeName)]
    public bool IsBackButton { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (context.ShouldSuppressChildContent())
        {
            return;
        }

        if (!IsBackButton)
        {
            return;
        }

        output.AppendCssClass("btn btn-outline-secondary btn-sm btn-icon");

        if (!output.Attributes.ContainsName("title"))
        {
            output.Attributes.SetAttribute("title", T("Admin.Common.BackToList"));
        }

        var icon = await iconRenderer.RenderAsync(await icons.GetIconAsync("arrow-left"), new IconOptions { Size = "lg" });
        output.Content.SetHtmlContent(icon);
    }
}