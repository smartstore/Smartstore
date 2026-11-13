using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared;

/// <summary>
/// Renders a module icon using either server-side SVG rendering or an image.
/// </summary>
[HtmlTargetElement("module-icon", Attributes = "model", TagStructure = TagStructure.WithoutEndTag)]
public class ModuleIconTagHelper : SmartTagHelper
{
    /// <summary>
    /// Gets or sets the resolved icon.
    /// </summary>
    public ModuleIcon Model { get; set; }

    /// <summary>
    /// Gets or sets an accessible name. Omit for decorative icons.
    /// </summary>
    public string Alt { get; set; }

    protected override string GenerateTagId(TagHelperContext context) => null;

    protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (Model == null)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.RemoveAll("model");
        output.Attributes.RemoveAll("alt");
        output.AppendCssClass("module-icon-display");

        if (Alt.HasValue())
        {
            output.Attributes.SetAttribute("role", "img");
            output.Attributes.SetAttribute("aria-label", Alt);
        }
        else
        {
            output.Attributes.SetAttribute("aria-hidden", "true");
        }

        if (Model.Address.HasValue())
        {
            output.Content.SetHtmlContent(await HtmlHelper.IconAsync(Model.Address));
        }
        else if (Model.ImageUrl.HasValue())
        {
            var image = new TagBuilder("img") { TagRenderMode = TagRenderMode.SelfClosing };
            image.AddCssClass("icon");
            image.Attributes["src"] = UrlHelper.Content(Model.ImageUrl);
            image.Attributes["alt"] = string.Empty;
            output.Content.SetHtmlContent(image);
        }
    }
}
