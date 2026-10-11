using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Content.Media.Icons;
using Smartstore.Web.Components;

namespace Smartstore.Facebook.Auth.Components;

public class FacebookAuthViewComponent : SmartViewComponent
{
    private readonly IIconService _icons;
    private readonly IIconRenderer _iconRenderer;
    private readonly FacebookOptions _facebookOptions;

    public FacebookAuthViewComponent(IOptionsMonitor<FacebookOptions> facebookOptions, IIconService icons, IIconRenderer iconRenderer)
    {
        _icons = icons;
        _iconRenderer = iconRenderer;
        _facebookOptions = facebookOptions.CurrentValue;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!_facebookOptions.AppId.HasValue() || !_facebookOptions.AppSecret.HasValue())
        {
            return Empty();
        }

        var returnUrl = HttpContext.Request.Query["returnUrl"].ToString();
        var href = Url.Action("ExternalLogin", "Identity", new { provider = "Facebook", returnUrl });
        var title = T("Plugins.ExternalAuth.Facebook.Login").Value;
        var link = new TagBuilder("a");
        link.Attributes["class"] = "btn btn-primary btn-block btn-lg btn-extauth btn-brand-facebook";
        link.Attributes["href"] = href;
        link.Attributes["rel"] = "nofollow";
        link.InnerHtml.AppendHtml(await _iconRenderer.RenderAsync(await _icons.GetIconAsync("facebook-f"), new IconOptions
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