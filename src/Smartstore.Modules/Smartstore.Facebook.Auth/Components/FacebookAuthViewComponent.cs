using System;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Smartstore.Web.Components;

namespace Smartstore.Facebook.Auth.Components
{
    public class FacebookAuthViewComponent : SmartViewComponent
    {
        private readonly FacebookOptions _facebookOptions;
        private readonly IUrlHelper _urlHelper;

        public FacebookAuthViewComponent(IOptionsSnapshot<FacebookOptions> facebookOptions, IUrlHelper urlHelper)
        {
            // INFO: (mh) (core) IOptionsSnapshot<T> ensures that an options instance is created once every request.
            // This way, the configurer is called on every request automatically (only once, the result is cached for the request duration).

            _facebookOptions = facebookOptions.Value;
            _urlHelper = urlHelper;
        }


        public IViewComponentResult Invoke()
        {
            if (!_facebookOptions.AppId.HasValue() && !_facebookOptions.AppSecret.HasValue())
            {
                return Empty();
            }      

            var returnUrl = HttpContext.Request.Query["returnUrl"].ToString();
            var href = _urlHelper.Action("ExternalLogin", "Identity", new { provider = "Facebook", returnUrl });
            var title = T("Plugins.Smartstore.Facebook.Auth.Login").Value;
            var html = $"<a class='btn btn-primary btn-block btn-lg btn-extauth btn-brand-facebook' href='{href}'>" +
                       $"<i class='fab fa-fw fa-lg fa-facebook-f'></i><span>{title}</span></a>";

            return HtmlContent(html);
        }
    }
}
