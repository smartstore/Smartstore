using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;

namespace Smartstore.Google.Auth.Components
{
    public class GoogleAuthViewComponent : SmartViewComponent
    {
        private readonly GoogleOptions _googleOptions;

        public GoogleAuthViewComponent(IOptionsMonitor<GoogleOptions> googleOptions)
        {
            _googleOptions = googleOptions.CurrentValue;
        }

        public IViewComponentResult Invoke()
        {
            if (!_googleOptions.ClientId.HasValue() || !_googleOptions.ClientSecret.HasValue())
            {
                return Empty();
            }

            var returnUrl = HttpContext.Request.Query["returnUrl"].ToString();
            var href = Url.Action("ExternalLogin", "Identity", new { provider = "Google", returnUrl });
            var title = T("Plugins.Smartstore.Google.Auth.Login").Value;
            var html = $"<a class='btn btn-primary btn-block btn-lg btn-extauth btn-brand-google' href='{href}'>" +
                       $"<i class='fab fa-fw fa-lg fa-google'></i><span>{title}</span></a>";

            return HtmlContent(html);
        }
    }
}
