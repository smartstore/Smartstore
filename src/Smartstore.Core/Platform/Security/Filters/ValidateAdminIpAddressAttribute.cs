using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Web;

namespace Smartstore.Core.Security
{
    public sealed class ValidateAdminIpAddressAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Creates a new instance of <see cref="ValidateAdminIpAddressAttribute"/>.
        /// </summary>
        /// <param name="validate">Set to <c>false</c> to override any controller-level <see cref="ValidateAdminIpAddressAttribute"/>.</param>
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
                    throw new AccessDeniedException();
                }
            }
        }
    }
}
