using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;

namespace Smartstore.Microsoft.Auth.Components
{
    public class MicrosoftAuthViewComponent : SmartViewComponent
    {
        private readonly MicrosoftAccountOptions _microsoftOptions;

        public MicrosoftAuthViewComponent(IOptionsMonitor<MicrosoftAccountOptions> microsoftOptions)
        {
            _microsoftOptions = microsoftOptions.CurrentValue;
        }


        public IViewComponentResult Invoke()
        {
            if (!_microsoftOptions.ClientId.HasValue() || !_microsoftOptions.ClientSecret.HasValue())
            {
                return Empty();
            }

            var returnUrl = HttpContext.Request.Query["returnUrl"].ToString();
            var href = Url.Action("ExternalLogin", "Identity", new { provider = "Microsoft", returnUrl });
            var title = T("Plugins.Smartstore.Microsoft.Auth.Login").Value;
            var html = $"<a class='btn btn-primary btn-block btn-lg btn-extauth btn-brand-microsoft' href='{href}'>" +
                       $"<i class='fab fa-fw fa-lg fa-microsoft font-weight-100'></i><span>{title}</span></a>";

            return HtmlContent(html);
        }
    }
}