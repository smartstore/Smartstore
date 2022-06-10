using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;

namespace Smartstore.Twitter.Auth.Components
{
    public class TwitterAuthViewComponent : SmartViewComponent
    {
        private readonly TwitterOptions _twitterOptions;
        private readonly IUrlHelper _urlHelper;

        public TwitterAuthViewComponent(IOptionsMonitor<TwitterOptions> twitterOptions, IUrlHelper urlHelper)
        {
            _twitterOptions = twitterOptions.CurrentValue;
            _urlHelper = urlHelper;
        }

        public IViewComponentResult Invoke()
        {
            if (!_twitterOptions.ConsumerKey.HasValue() || !_twitterOptions.ConsumerSecret.HasValue())
            {
                return Empty();
            }

            var returnUrl = HttpContext.Request.Query["returnUrl"].ToString();
            var href = _urlHelper.Action("ExternalLogin", "Identity", new { provider = "Twitter", returnUrl });
            var title = T("Plugins.Smartstore.Twitter.Auth.Login").Value;
            var html = $"<a class='btn btn-primary btn-block btn-lg btn-extauth btn-brand-twitter' href='{href}'>" +
                       $"<i class='fab fa-fw fa-lg fa-twitter'></i><span>{title}</span></a>";

            return HtmlContent(html);
        }
    }
}
