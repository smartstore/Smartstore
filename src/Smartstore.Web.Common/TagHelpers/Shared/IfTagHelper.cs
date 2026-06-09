using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared;

[HtmlTargetElement("*", Attributes = IfAttributeName)]
public class IfTagHelper : TagHelper
{
    public static readonly object SuppressChildContentKey = new();

    const string IfAttributeName = "sm-if";

    public override int Order => int.MinValue;

    /// <summary>
    /// A condition to check before outputting the tag.
    /// <c>false</c> will suppress the output completely.
    /// </summary>
    [HtmlAttributeName(IfAttributeName)]
    public bool Condition { get; set; } = true;

    public override void Init(TagHelperContext context)
    {
        if (!Condition)
        {
            // Set the flag during Init so that peer TagHelpers on the same element
            // (e.g. TabTagHelper, which runs Init after us due to Order) can bail out
            // before performing irreversible side-effects such as registering with a parent.
            context.Items[SuppressChildContentKey] = true;
        }
    }

    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (!Condition || context.ShouldSuppressChildContent())
        {
            output.SuppressOutput();
        }

        return Task.CompletedTask;
    }
}