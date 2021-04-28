using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Web;

namespace Smartstore.Core.Security
{
    public sealed class ValidateAdminIpAddressAttribute : TypeFilterAttribute
    {
        public ValidateAdminIpAddressAttribute(bool validate = true)
            : base(typeof(ValidateAdminIpAddressFilter))
        {
            Validate = validate;
            Arguments = new object[] { validate };
        }

        public bool Validate { get; }

        class ValidateAdminIpAddressFilter : IAuthorizationFilter
        {
            private readonly IWebHelper _webHelper;
            private readonly SecuritySettings _securitySettings;
            private readonly bool _validate;
            
            public ValidateAdminIpAddressFilter(IWebHelper webHelper, SecuritySettings securitySettings, bool validate)
            {
                _webHelper = webHelper;
                _securitySettings = securitySettings;
                _validate = validate;
            }

            public void OnAuthorization(AuthorizationFilterContext context)
            {
                if (!_validate)
                {
                    return;
                }

                // Prevent lockout
                if (context.HttpContext.Connection.IsLocal())
                {
                    return;
                }    

                var overrideFilter = context.ActionDescriptor.FilterDescriptors
                    .Where(x => x.Scope == FilterScope.Action)
                    .Select(x => x.Filter)
                    .OfType<ValidateAdminIpAddressAttribute>()
                    .FirstOrDefault();

                if (overrideFilter?.Validate == false)
                {
                    return;
                }

                var allowedIpAddresses = _securitySettings.AdminAreaAllowedIpAddresses;
                bool allow = allowedIpAddresses == null || allowedIpAddresses.Count == 0;
                if (!allow)
                {
                    var currentIpAddress = _webHelper.GetClientIpAddress().ToString();
                    allow = allowedIpAddresses.Any(ip => ip.EqualsNoCase(currentIpAddress));
                }

                if (!allow)
                {
                    var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
                    var action = actionDescriptor?.ActionName;
                    var controller = actionDescriptor?.ControllerName;

                    if (!(action.EqualsNoCase("AccessDenied") && controller.EqualsNoCase("Security")))
                    {
                        // Redirect to 'Access denied' page, but avoid infinite redirection
                        context.Result = new RedirectToActionResult("AccessDenied", "Security", context.RouteData.Values);
                    }
                }
            }
        }
    }
}
