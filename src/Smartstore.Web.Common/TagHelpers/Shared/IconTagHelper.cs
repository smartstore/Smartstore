using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Content.Media.Icons;

namespace Smartstore.Web.TagHelpers.Shared;

/// <summary>
/// Renders a local IconKit icon as inline SVG. Presentation is controlled by icon CSS classes and variables.
/// </summary>
/// <param name="iconService">Resolves icon addresses and supplies validated, cached SVG payloads.</param>
[HtmlTargetElement("icon", Attributes = NameAttributeName, TagStructure = TagStructure.WithoutEndTag)]
[OutputElementHint("svg")]
public class IconTagHelper(IIconService iconService) : SmartTagHelper
{
    const string NameAttributeName = "name";
    const string LibraryAttributeName = "library";
    const string VariantAttributeName = "variant";

    /// <summary>
    /// Gets or sets the icon or conceptual name, optionally qualified as library:name@variant.
    /// </summary>
    [HtmlAttributeName(NameAttributeName)]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the library system name or short name. Omission uses the address or configured default.
    /// </summary>
    [HtmlAttributeName(LibraryAttributeName)]
    public string Library { get; set; }

    /// <summary>
    /// Gets or sets the variant name or short name. Omission uses the address or effective library default.
    /// </summary>
    [HtmlAttributeName(VariantAttributeName)]
    public string Variant { get; set; }

    /// <inheritdoc />
    protected override string GenerateTagId(TagHelperContext context) => null;

    /// <inheritdoc />
    protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
    {
        Guard.NotEmpty(Name);

        var svg = await iconService.GetSvgAsync(Name, Library, Variant, ViewContext.HttpContext.RequestAborted);
        if (svg == null)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.RemoveAll(NameAttributeName);
        output.Attributes.RemoveAll(LibraryAttributeName);
        output.Attributes.RemoveAll(VariantAttributeName);

        // Expose the resolved identity, including mapping and default library/variant selection.
        output.Attributes.SetAttribute("data-icon", svg.Address);

        // Preserve source root attributes, then append the
        // caller's style declarations without changing the cached payload.
        foreach (var attribute in svg.RootAttributes)
        {
            if (attribute.Key == "class")
            {
                output.AppendCssClass(attribute.Value);
            }
            else if (attribute.Key == "style" && output.Attributes.TryGetAttribute("style", out var style))
            {
                output.Attributes.SetAttribute("style", attribute.Value.TrimEnd(';'));
                var callerStyles = style.ValueAsString();
                if (!string.IsNullOrWhiteSpace(callerStyles))
                {
                    output.AddCssStyles(callerStyles);
                }
            }
            else if (!output.Attributes.ContainsName(attribute.Key))
            {
                output.Attributes.SetAttribute(attribute.Key, attribute.Value);
            }
        }

        output.AppendCssClass("icon");
        output.Attributes.SetAttribute("xmlns", "http://www.w3.org/2000/svg");
        output.Attributes.SetAttribute("viewBox", svg.ViewBox);
        output.Attributes.SetAttribute("focusable", "false");

        // Icons next to text are decorative by default. Callers can give standalone icons
        // an accessible name with aria-label/aria-labelledby and explicitly control aria-hidden.
        var hasLabel = output.Attributes.ContainsName("aria-label") || output.Attributes.ContainsName("aria-labelledby");
        if (!output.Attributes.ContainsName("aria-hidden") && !hasLabel)
        {
            output.Attributes.SetAttribute("aria-hidden", "true");
        }

        if (hasLabel && !output.Attributes.ContainsName("role"))
        {
            output.Attributes.SetAttribute("role", "img");
        }

        // Only the service's validated child markup bypasses encoding. Root attribute strings
        // stay ordinary TagHelper attributes and are encoded by Razor when the SVG is written.
        output.Content.SetHtmlContent(svg.Content);
    }
}
