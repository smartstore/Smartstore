using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Content.Media.Icons;
using Smartstore.Web.Components;

namespace Smartstore.Twitter.Auth.Components;

public class TwitterAuthViewComponent : SmartViewComponent
{
    private readonly IIconService _icons;
    private readonly IIconRenderer _iconRenderer;
    private readonly TwitterOptions _twitterOptions;

    public TwitterAuthViewComponent(IOptionsMonitor<TwitterOptions> twitterOptions, IIconService icons, IIconRenderer iconRenderer)
    {
        _icons = icons;
        _iconRenderer = iconRenderer;
        _twitterOptions = twitterOptions.CurrentValue;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!_twitterOptions.ConsumerKey.HasValue() || !_twitterOptions.ConsumerSecret.HasValue())
        {
            return Empty();
        }

        var returnUrl = HttpContext.Request.Query["returnUrl"].ToString();
        var href = Url.Action("ExternalLogin", "Identity", new { provider = "Twitter", returnUrl });
        var title = T("Plugins.ExternalAuth.Twitter.Login").Value;
        var link = new TagBuilder("a");
        link.Attributes["class"] = "btn btn-primary btn-block btn-lg btn-extauth btn-brand-x-twitter";
        link.Attributes["href"] = href;
        link.Attributes["rel"] = "nofollow";
        link.InnerHtml.AppendHtml(await _iconRenderer.RenderAsync(await _icons.GetIconAsync("twitter-x"), new IconOptions
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