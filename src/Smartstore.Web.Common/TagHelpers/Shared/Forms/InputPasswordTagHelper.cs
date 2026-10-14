using Autofac;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;
using Smartstore.Data;
using Smartstore.IconKit;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared;

[HtmlTargetElement("input", Attributes = "[type=password]")]
public class InputPasswordTagHelper : TagHelper
{
    const string EnableVisibilityToggleAttributeName = "sm-enable-visibility-toggle";

    private readonly IHtmlHelper _htmlHelper;

    public InputPasswordTagHelper(IHtmlHelper htmlHelper)
    {
        _htmlHelper = htmlHelper;
    }

    // Must comer AFTER AspNetCore original TagHelper (Input | Select | TextArea)
    public override int Order => 100;

    /// <summary>
    /// Show visibility toggle button. Default = true.
    /// </summary>
    [HtmlAttributeName(EnableVisibilityToggleAttributeName)]
    public bool EnableVisibilityToggle { get; set; } = true;

    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (context.ShouldSuppressChildContent())
        {
            return;
        }

        if (EnableVisibilityToggle)
        {
            output.PreElement.AppendHtml("<div class=\"toggle-pwd-group\">");

            string htmlAttributes = null;
            var isInstalled = DataSettings.DatabaseIsInstalled();
            if (isInstalled)
            {
                var T = ViewContext.HttpContext.GetServiceScope().Resolve<Localizer>();
                var id = (output.Attributes.TryGetAttribute("id", out var idAttr) ? idAttr.ValueAsString() : null).EmptyNull();

                htmlAttributes = $"aria-pressed=\"false\" aria-controls=\"{id}\" aria-label=\"{T("Aria.Label.ShowPassword")}\"";
            }
            else
            {
                htmlAttributes = "aria-hidden=\"true\"";
            }

            if (_htmlHelper is IViewContextAware contextAware)
            {
                contextAware.Contextualize(ViewContext);
            }

            var hiddenIcon = await _htmlHelper.IconAsync("eye-off", options: new IconOptions
            {
                ForceInline = !isInstalled,
                Attributes = { ["class"] = "pwd-hidden" }
            });
            var visibleIcon = await _htmlHelper.IconAsync("eye", options: new IconOptions
            {
                ForceInline = !isInstalled,
                Attributes = { ["class"] = "pwd-visible" }
            });

            output.PostElement.AppendHtml($"<button type=\"button\" class=\"btn-toggle-pwd\" tabindex=\"-1\" {htmlAttributes}>");
            output.PostElement.AppendHtml(hiddenIcon);
            output.PostElement.AppendHtml(visibleIcon);
            output.PostElement.AppendHtml("</button></div>");
        }
    }
}