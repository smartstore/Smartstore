using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Smartstore.IconKit;
using Smartstore.Web.Components;

namespace Smartstore.Google.Auth.Components;

public class GoogleAuthViewComponent : SmartViewComponent
{
    private readonly IIconRenderer _iconRenderer;
    private readonly GoogleOptions _googleOptions;

    public GoogleAuthViewComponent(IOptionsMonitor<GoogleOptions> googleOptions, IIconRenderer iconRenderer)
    {
        _iconRenderer = iconRenderer;
        _googleOptions = googleOptions.CurrentValue;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!_googleOptions.ClientId.HasValue() || !_googleOptions.ClientSecret.HasValue())
        {
            return Empty();
        }

        var returnUrl = HttpContext.Request.Query["returnUrl"].ToString();
        var href = Url.Action("ExternalLogin", "Identity", new { provider = "Google", returnUrl });
        var title = T("Plugins.Smartstore.Google.Auth.Login").Value;
        var link = new TagBuilder("a");
        link.Attributes["class"] = "btn btn-primary btn-block btn-lg btn-extauth btn-brand-google";
        link.Attributes["href"] = href;
        link.Attributes["rel"] = "nofollow";
        link.InnerHtml.AppendHtml(await _iconRenderer.RenderAsync(IconExpression.Parse("google?fw&size=lg")));
        var label = new TagBuilder("span");
        label.InnerHtml.Append(title);
        link.InnerHtml.AppendHtml(label);

        return HtmlContent(link);
    }
}