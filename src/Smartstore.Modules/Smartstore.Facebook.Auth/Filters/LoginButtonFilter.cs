using System;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets;

namespace Smartstore.Facebook.Auth.Filters
{
    public class LoginButtonFilter : IResultFilter
    {
        private readonly FacebookOptions _facebookOptions;
        private readonly IWidgetProvider _widgetProvider;
        private readonly ILocalizationService _localizationService;
        private readonly Lazy<IUrlHelper> _urlHelper;

        public LoginButtonFilter(
            IOptions<FacebookOptions> facebookOptions,
            IWidgetProvider widgetProvider,
            ILocalizationService localizationService,
            Lazy<IUrlHelper> urlHelper)
        {
            _facebookOptions = facebookOptions.Value;
            _widgetProvider = widgetProvider;
            _localizationService = localizationService;
            _urlHelper = urlHelper;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (!_facebookOptions.AppId.HasValue() && !_facebookOptions.AppSecret.HasValue())
                return;

            // Should only run on a full view rendering result or HTML ContentResult.
            if (!filterContext.Result.IsHtmlViewResult())
                return;

            var returnUrl = filterContext.HttpContext.Request.Query["returnUrl"].ToString();
            var href = _urlHelper.Value.Action("ExternalLogin", "Identity", new { provider = "Facebook", returnUrl });
            var title = _localizationService.GetResource("Plugins.ExternalAuth.Facebook.Login");
            var html = $"<a class='btn btn-primary btn-icon btn-lg btn-extauth btn-brand-facebook' data-toggle='tooltip' title='{ title }' href='{ href }'>" +
                       $"<i class='fab fa-fw fa-lg fa-facebook'></i></a>";

            _widgetProvider.RegisterHtml(new[] { "external_auth_buttons" }, new HtmlString(html));
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
