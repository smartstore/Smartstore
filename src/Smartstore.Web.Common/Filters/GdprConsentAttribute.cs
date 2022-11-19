using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Identity;
using Smartstore.Core.Logging;

namespace Smartstore.Web.Filters
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

        class GdprConsentFilter : IAsyncActionFilter, IResultFilter
        {
            private readonly ICommonServices _services;
            private readonly IWidgetProvider _widgetProvider;
            private readonly PrivacySettings _privacySettings;
            private readonly INotifier _notifier;

            public GdprConsentFilter(
                ICommonServices services,
                IWidgetProvider widgetProvider,
                PrivacySettings privacySettings,
                INotifier notifier)
            {
                _services = services;
                _widgetProvider = widgetProvider;
                _privacySettings = privacySettings;
                _notifier = notifier;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                if (!_privacySettings.DisplayGdprConsentOnForms)
                {
                    await next();
                    return;
                }

                var httpContext = context.HttpContext;

                if (httpContext.Request.IsPost())
                {
                    var hasConsentedToGdpr = httpContext.Request.Form["GdprConsent"].ToString();

                    if (hasConsentedToGdpr.HasValue())
                    {
                        var customer = _services.WorkContext.CurrentCustomer;

                        // Set flag which can be accessed in corresponding action
                        httpContext.Items.Add("GdprConsent", hasConsentedToGdpr.Contains("true"));

                        if (hasConsentedToGdpr.Contains("true"))
                        {
                            customer.GenericAttributes.HasConsentedToGdpr = true;
                            await customer.GenericAttributes.SaveChangesAsync();
                        }
                        else
                        {
                            if (!httpContext.Request.IsAjax())
                            {
                                // Add a validation message
                                context.ModelState.AddModelError(string.Empty, _services.Localization.GetResource("Gdpr.Consent.ValidationMessage"));
                            }
                            else
                            {
                                // Notify
                                _notifier.Error(_services.Localization.GetResource("Gdpr.Consent.ValidationMessage"));
                            }
                        }
                    }
                }

                await next();
            }

            public void OnResultExecuting(ResultExecutingContext context)
            {
                if (!_privacySettings.DisplayGdprConsentOnForms)
                    return;

                if (context.HttpContext.Items.Keys.Contains("GdprConsentRendered"))
                    return;

                var result = context.Result;

                // should only run on a full view rendering result or HTML ContentResult
                if (!result.IsHtmlViewResult())
                    return;

                _widgetProvider.RegisterWidget("gdpr_consent",
                    new ComponentWidget("GdprConsent", new { isSmall = false }));

                _widgetProvider.RegisterWidget("gdpr_consent_small",
                    new ComponentWidget("GdprConsent", new { isSmall = true }));

                context.HttpContext.Items["GdprConsentRendered"] = true;
            }

            public void OnResultExecuted(ResultExecutedContext context)
            {
            }
        }
    }
}
