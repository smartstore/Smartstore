using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared;

/// <summary>
/// A transparent wrapper tag helper that renders its child content without any wrapping element.
/// Use <c>&lt;fragment&gt;</c> to group content that cannot be expressed as a plain HTML element
/// inside a parent tag helper with child restrictions (e.g. <c>&lt;toolbar&gt;</c>,
/// <c>&lt;toolbar-group&gt;</c>, <c>&lt;row-commands&gt;</c>).
/// </summary>
[HtmlTargetElement("fragment")]
public class FragmentTagHelper : TagHelper
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        _ = await output.GetChildContentAsync();
    }
}
