using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Smartstore.Web.Components;

namespace Smartstore.Google.Auth.Components
{
    public class GoogleAuthViewComponent : SmartViewComponent
    {
        private readonly GoogleOptions _googleOptions;
        private readonly IUrlHelper _urlHelper;

        public GoogleAuthViewComponent(IOptionsMonitor<GoogleOptions> googleOptions, IUrlHelper urlHelper)
        {
            _googleOptions = googleOptions.CurrentValue;
            _urlHelper = urlHelper;
        }

        public IViewComponentResult Invoke()
        {
            if (!_googleOptions.ClientId.HasValue() && !_googleOptions.ClientSecret.HasValue())
            {
                return Empty();
            }

            var returnUrl = HttpContext.Request.Query["returnUrl"].ToString();
            var href = _urlHelper.Action("ExternalLogin", "Identity", new { provider = "Google", returnUrl });
            var title = T("Plugins.Smartstore.Google.Auth.Login").Value;
            var html = $"<a class='btn btn-primary btn-block btn-lg btn-extauth btn-brand-google' href='{href}'>" +
                       $"<i class='fab fa-fw fa-lg fa-google'></i><span>{title}</span></a>";

            return HtmlContent(html);
        }
    }
}
