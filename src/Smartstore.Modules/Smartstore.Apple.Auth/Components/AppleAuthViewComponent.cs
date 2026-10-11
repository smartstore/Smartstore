using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Content.Media.Icons;
using Smartstore.Web.Components;

namespace Smartstore.Apple.Auth.Components;

public class AppleAuthViewComponent : SmartViewComponent
{
    private readonly IIconService _icons;
    private readonly IIconRenderer _iconRenderer;
    private readonly AppleAuthenticationOptions _appleOptions;
    private readonly AppleExternalAuthSettings _settings;


    public AppleAuthViewComponent(IOptionsMonitor<AppleAuthenticationOptions> appleOptions, AppleExternalAuthSettings settings, IIconService icons, IIconRenderer iconRenderer)
    {
        _icons = icons;
        _iconRenderer = iconRenderer;
        _appleOptions = appleOptions.CurrentValue;
        _settings = settings;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!_appleOptions.ClientId.HasValue()
            || !_appleOptions.TeamId.HasValue()
            || !_appleOptions.KeyId.HasValue()
            || !_appleOptions.ClientSecret.HasValue()
            || !_settings.PrivateKey.HasValue())
        {
            return Empty();
        }

        var returnUrl = HttpContext.Request.Query["returnUrl"].ToString();
        var href = Url.Action("ExternalLogin", "Identity", new { provider = "Apple", returnUrl });
        var title = T("Plugins.Smartstore.Apple.Auth.Login").Value;
        var link = new TagBuilder("a");
        link.Attributes["class"] = "btn btn-secondary btn-block btn-lg btn-extauth";
        link.Attributes["href"] = href;
        link.Attributes["rel"] = "nofollow";
        link.InnerHtml.AppendHtml(await _iconRenderer.RenderAsync(await _icons.GetIconAsync("apple"), new IconOptions
        {
            FixedWidth = true,
            Size = "lg"
        }));
        var label = new TagBuilder("span");
        label.InnerHtml.Append(title);
        link.InnerHtml.AppendHtml(label);

        return HtmlContent(link);
    }
}