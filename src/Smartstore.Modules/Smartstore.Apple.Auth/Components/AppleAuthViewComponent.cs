using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;

namespace Smartstore.Apple.Auth.Components
{
    public class AppleAuthViewComponent : SmartViewComponent
    {
        private readonly AppleAuthenticationOptions _appleOptions;
        private readonly AppleExternalAuthSettings _settings;
        

        public AppleAuthViewComponent(IOptionsMonitor<AppleAuthenticationOptions> appleOptions, AppleExternalAuthSettings settings)
        {
            _appleOptions = appleOptions.CurrentValue;
            _settings = settings;
        }

        public IViewComponentResult Invoke()
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
            var html = $"<a class='btn btn-secondary btn-block btn-lg btn-extauth' href='{href}' rel='nofollow'>" +
                       $"<i class='fab fa-fw fa-lg fa-apple' aria-hidden='true'></i><span>{title}</span></a>";

            return HtmlContent(html);
        }
    }
}

