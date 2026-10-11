using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Content.Media.Icons;
using Smartstore.Web.Components;

namespace Smartstore.Microsoft.Auth.Components;

public class MicrosoftAuthViewComponent : SmartViewComponent
{
    private readonly IIconService _icons;
    private readonly IIconRenderer _iconRenderer;
    private readonly MicrosoftAccountOptions _microsoftOptions;

    public MicrosoftAuthViewComponent(IOptionsMonitor<MicrosoftAccountOptions> microsoftOptions, IIconService icons, IIconRenderer iconRenderer)
    {
        _icons = icons;
        _iconRenderer = iconRenderer;
        _microsoftOptions = microsoftOptions.CurrentValue;
    }


    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!_microsoftOptions.ClientId.HasValue() || !_microsoftOptions.ClientSecret.HasValue())
        {
            return Empty();
        }

        var returnUrl = HttpContext.Request.Query["returnUrl"].ToString();
        var href = Url.Action("ExternalLogin", "Identity", new { provider = "Microsoft", returnUrl });
        var title = T("Plugins.Smartstore.Microsoft.Auth.Login").Value;
        var link = new TagBuilder("a");
        link.Attributes["class"] = "btn btn-primary btn-block btn-lg btn-extauth btn-brand-microsoft";
        link.Attributes["href"] = href;
        link.Attributes["rel"] = "nofollow";
        link.InnerHtml.AppendHtml(await _iconRenderer.RenderAsync(await _icons.GetIconAsync("microsoft"), new IconOptions
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