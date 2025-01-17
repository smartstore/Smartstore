using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Localization;
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

        class GdprConsentFilter : IAsyncActionFilter, IResultFilter
        {
            private readonly IWorkContext _workContext;
            private readonly IWidgetProvider _widgetProvider;
            private readonly PrivacySettings _privacySettings;
            private readonly INotifier _notifier;

            public GdprConsentFilter(
                IWorkContext workContext,
                IWidgetProvider widgetProvider,
                PrivacySettings privacySettings,
                INotifier notifier,
                Localizer localizer)
            {
                _workContext = workContext;
                _widgetProvider = widgetProvider;
                _privacySettings = privacySettings;
                _notifier = notifier;
                T = localizer;
            }

            public Localizer T { get; set; } = NullLocalizer.Instance;

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                if (!_privacySettings.DisplayGdprConsentOnForms)
                {
                    await next();
                    return;
                }

                var request = context.HttpContext.Request;
                if (request.IsPost() && request.HasFormContentType)
                {
                    var hasConsentedToGdpr = request.Form["GdprConsent"].ToString();
                    if (hasConsentedToGdpr.HasValue())
                    {
                        // Set flag which can be accessed in corresponding action.
                        context.HttpContext.Items.Add("GdprConsent", hasConsentedToGdpr.Contains("true"));

                        if (hasConsentedToGdpr.Contains("true"))
                        {
                            var customer = _workContext.CurrentCustomer;
                            customer.GenericAttributes.HasConsentedToGdpr = true;
                            await customer.GenericAttributes.SaveChangesAsync();
                        }
                        else
                        {
                            if (!request.IsAjax())
                            {
                                context.ModelState.AddModelError(string.Empty, T("Gdpr.Consent.ValidationMessage"));
                            }
                            else
                            {
                                _notifier.Error(T("Gdpr.Consent.ValidationMessage"));
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

                if (context.HttpContext.Items.ContainsKey("GdprConsentRendered"))
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
