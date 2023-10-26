using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;

namespace Smartstore.Twitter.Auth.Components
{
    public class TwitterAuthViewComponent : SmartViewComponent
    {
        private readonly TwitterOptions _twitterOptions;

        public TwitterAuthViewComponent(IOptionsMonitor<TwitterOptions> twitterOptions)
        {
            _twitterOptions = twitterOptions.CurrentValue;
        }

        public IViewComponentResult Invoke()
        {
            if (!_twitterOptions.ConsumerKey.HasValue() || !_twitterOptions.ConsumerSecret.HasValue())
            {
                return Empty();
            }

            var returnUrl = HttpContext.Request.Query["returnUrl"].ToString();
            var href = Url.Action("ExternalLogin", "Identity", new { provider = "Twitter", returnUrl });
            var title = T("Plugins.ExternalAuth.Twitter.Login").Value;
            var html = $"<a class='btn btn-primary btn-block btn-lg btn-extauth btn-brand-x-twitter' href='{href}'>" +
                       $"<i class='fab fa-fw fa-lg fa-x-twitter'></i><span>{title}</span></a>";

            return HtmlContent(html);
        }
    }
}
