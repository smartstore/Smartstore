using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Web;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Checks whether captcha is valid and - if not - outputs a notification.
    /// </summary>
    public class ValidateHoneypotAttribute : TypeFilterAttribute
    {
        public ValidateHoneypotAttribute() 
            : base(typeof(ValidateHoneypotFilter))
        {
        }

        class ValidateHoneypotFilter : IAuthorizationFilter
        {
            private readonly HoneypotProtector _honeypotProtector;
            private readonly SecuritySettings _securitySettings;
            private readonly IWebHelper _webHelper;
            private readonly ILogger _logger;

            public ValidateHoneypotFilter(
                HoneypotProtector honeypotProtector,
                SecuritySettings securitySettings,
                IWebHelper webHelper,
                ILogger<ValidateHoneypotFilter> logger)
            {
                _honeypotProtector = honeypotProtector;
                _securitySettings = securitySettings;
                _webHelper = webHelper;
                _logger = logger;
            }

            public void OnAuthorization(AuthorizationFilterContext context)
            {
                if (!_securitySettings.EnableHoneypotProtection)
                    return;

                var isBot = _honeypotProtector.IsBot();
                if (!isBot)
                    return;

                _logger.Warn("Honeypot detected a bot and rejected the request.");

                var redirectUrl = _webHelper.GetCurrentPageUrl(true);
                context.Result = new RedirectResult(redirectUrl);
            }
        }
    }
}
