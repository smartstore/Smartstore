using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets;

namespace Smartstore.Facebook.Auth.Filters
{
    public class LoginButtonFilter : IResultFilter
    {
        private readonly FacebookExternalAuthSettings _facebookSettings;
        private readonly Lazy<IWidgetProvider> _widgetProvider;
        private readonly Lazy<ILocalizationService> _localizationService;
        private readonly Lazy<IUrlHelper> _urlHelper;

        public LoginButtonFilter(
            FacebookExternalAuthSettings facebookSettings,
            Lazy<IWidgetProvider> widgetProvider,
            Lazy<ILocalizationService> localizationService,
            Lazy<IUrlHelper> urlHelper)
        {
            _facebookSettings = facebookSettings;
            _widgetProvider = widgetProvider;
            _localizationService = localizationService;
            _urlHelper = urlHelper;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (!_facebookSettings.ClientKeyIdentifier.HasValue() && !_facebookSettings.ClientSecret.HasValue())
                return;

            // Should only run on a full view rendering result or HTML ContentResult.
            if (!filterContext.Result.IsHtmlViewResult())
                return;

            // Get out if we're not on login page.
            if (filterContext.RouteData.Values.GetControllerName() != "Identity")
                return;

            var returnUrl = filterContext.HttpContext.Request.Query["returnUrl"].ToString();
            var href = _urlHelper.Value.Action("ExternalLogin", "Identity", new { provider = "Facebook", returnUrl });
            var title = _localizationService.Value.GetResource("Plugins.ExternalAuth.Facebook.Login");
            var html = $"<a class='btn btn-primary btn-icon btn-lg btn-extauth btn-brand-facebook' data-toggle='tooltip' title='{ title }' href='{ href }'>" +
                       $"<i class='fab fa-fw fa-lg fa-facebook'></i></a>";

            _widgetProvider.Value.RegisterHtml(new[] { "external_auth_buttons" }, new HtmlString(html));
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
