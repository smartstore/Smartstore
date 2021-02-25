using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Logging;
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
            private readonly ICommonServices _services;
            private readonly IWidgetProvider _widgetProvider;
            private readonly PrivacySettings _privacySettings;
            private readonly IGenericAttributeService _genericAttributeService;
            private readonly INotifier _notifier;

            public GdprConsentFilter(ICommonServices services, 
                IWidgetProvider widgetProvider, 
                PrivacySettings privacySettings,
                IGenericAttributeService genericAttributeService,
                INotifier notifier)
            {
                _services = services;
                _widgetProvider = widgetProvider;
                _privacySettings = privacySettings;
                _genericAttributeService = genericAttributeService;
                _notifier = notifier;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                if (!_privacySettings.DisplayGdprConsentOnForms)
                {
                    await next();
                    return;
                }

                if (context?.ActionDescriptor == null || context?.HttpContext?.Request == null)
                {
                    await next();
                    return;
                }

                if (context.HttpContext.Request.Method.Equals("GET"))
                {
                    await next();
                    return;
                }

                var customer = _services.WorkContext.CurrentCustomer;

                // TODO: (mh) (core) remove test code
                //var db = _services.DbContext;
                //customer = db.Customers.FindById(1);

                var hasConsentedToGdpr = context.HttpContext.Request.Form["GdprConsent"].FirstOrDefault();

                if (context.HttpContext.Request.Method.Equals("POST") && hasConsentedToGdpr.HasValue())
                {
                    // set flag which can be accessed in corresponding action
                    context.HttpContext.Items.Add("GdprConsent", hasConsentedToGdpr.Contains("true"));

                    if (hasConsentedToGdpr.Contains("true"))
                    {
                        var attrs = _genericAttributeService.GetAttributesForEntity(customer);
                        attrs.Set(SystemCustomerAttributeNames.HasConsentedToGdpr, true);
                    }
                    else
                    {
                        if (!context.HttpContext.Request.IsAjaxRequest())
                        {
                            // add a validation message
                            context.ModelState.AddModelError("", _services.Localization.GetResource("Gdpr.Consent.ValidationMessage"));
                        }
                        else
                        {
                            // notify
                            _notifier.Error(_services.Localization.GetResource("Gdpr.Consent.ValidationMessage"));
                        }

                        await next();
                        return;
                    }
                }

                await next();
            }

            public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
            {
                if (!_privacySettings.DisplayGdprConsentOnForms)
                    return;

                if (context.HttpContext.Items.Keys.Contains("GdprConsentRendered"))
                    return;

                var result = context.Result;

                // should only run on a full view rendering result or HTML ContentResult
                if (!result.IsHtmlViewResult())
                    return;

                var widget = new ComponentWidgetInvoker("GdprConsent", new { isSmall = true });
                _widgetProvider.RegisterWidget(new[] { "gdpr_consent", "gdpr_consent_small" }, widget);

                context.HttpContext.Items["GdprConsentRendered"] = true;

                await next();
            }
        }
    }
}
