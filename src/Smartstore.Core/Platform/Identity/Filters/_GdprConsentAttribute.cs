using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Widgets;

namespace Smartstore.Core.Identity
{
    /// <summary>
    /// Requires GDPR consent for the current action.
    /// </summary>
    public sealed class GdprConsentAttribute : TypeFilterAttribute
    {
        public GdprConsentAttribute()
            : base(typeof(GdprConsentFilter))
        {
        }

        class GdprConsentFilter : IAsyncActionFilter, IAsyncResultFilter
        {
            // TODO: (mh) (core) Implement GdprConsentFilter

            private readonly ICommonServices _services;
            private readonly IWidgetProvider _widgetProvider;
            private readonly PrivacySettings _privacySettings;

            public GdprConsentFilter(ICommonServices services, IWidgetProvider widgetProvider, PrivacySettings privacySettings)
            {
                _services = services;
                _widgetProvider = widgetProvider;
                _privacySettings = privacySettings;
            }

            public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                return next();
            }

            public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
            {
                return next();
            }
        }
    }
}
