using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;

namespace Smartstore.Core.Stores
{
    public sealed class CheckStoreClosedAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Creates a new instance of <see cref="CheckStoreClosedAttribute"/>.
        /// </summary>
        /// <param name="check">Set to <c>false</c> to override any controller-level <see cref="CheckStoreClosedAttribute"/>.</param>
        public CheckStoreClosedAttribute(bool check = true)
            : base(typeof(CheckStoreClosedFilter))
        {
            Check = check;
            Arguments = new object[] { check };
        }

        public bool Check { get; }

        class CheckStoreClosedFilter : IAuthorizationFilter
        {
            private readonly IWorkContext _workContext;
            private readonly INotifier _notifier;
            private readonly StoreInformationSettings _storeInfoSettings;
            private readonly bool _check;

            public CheckStoreClosedFilter(
                IWorkContext workContext,
                INotifier notifier,
                Localizer localizer,
                StoreInformationSettings storeInfoSettings, bool check)
            {
                _workContext = workContext;
                _notifier = notifier;
                _storeInfoSettings = storeInfoSettings;
                T = localizer;

                _check = check;
            }

            public Localizer T { get; set; } = NullLocalizer.Instance;

            public void OnAuthorization(AuthorizationFilterContext context)
            {
                if (!_storeInfoSettings.StoreClosed)
                {
                    return;
                }

                if (!_check)
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
                    .OfType<CheckStoreClosedAttribute>()
                    .FirstOrDefault();

                if (overrideFilter?.Check == false)
                {
                    return;
                }

                var customer = _workContext.CurrentCustomer;

                if (_storeInfoSettings.StoreClosedAllowForAdmins && (customer.IsAdmin() || customer.IsSuperAdmin()))
                {
                    // Allow admin access
                }
                else
                {
                    if (context.HttpContext.Request.IsAjax())
                    {
                        var storeClosedMessage = "{0} {1}".FormatCurrentUI(
                            T("StoreClosed"),
                            T("StoreClosed.Hint"));

                        _notifier.Error(storeClosedMessage);
                    }
                    else
                    {
                        context.Result = new RedirectToRouteResult("StoreClosed", null);
                    }
                }
            }
        }
    }
}
