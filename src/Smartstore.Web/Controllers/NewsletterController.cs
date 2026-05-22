using Smartstore.Core.Identity;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Seo.Routing;
using Smartstore.Web.Models.Newsletter;

namespace Smartstore.Web.Controllers;

public class NewsletterController : PublicController
{
    private readonly IWorkContext _workContext;
    private readonly INewsletterSubscriptionService _newsletterSubscriptionService;
    private readonly PrivacySettings _privacySettings;

    public NewsletterController(
        IWorkContext workContext,
        INewsletterSubscriptionService newsletterSubscriptionService,
        PrivacySettings privacySettings)
    {
        _workContext = workContext;
        _newsletterSubscriptionService = newsletterSubscriptionService;
        _privacySettings = privacySettings;
    }

    [HttpPost]
    [GdprConsent, DisallowRobot, ValidateHoneypot]
    [LocalizedRoute("newsletter/subscribe", Name = "SubscribeNewsletter")]
    public async Task<IActionResult> Subscribe(bool subscribe, string email)
    {
        var customer = _workContext.CurrentCustomer;
        var hasConsentedToGdpr = customer.GenericAttributes.HasConsentedToGdpr;
        var hasConsented = ViewData["GdprConsent"] != null ? (bool)ViewData["GdprConsent"] : hasConsentedToGdpr;

        if (!hasConsented && _privacySettings.DisplayGdprConsentOnForms)
        {
            return Json(new { Success = false, Result = string.Empty });
        }

        if (!email.IsEmail())
        {
            return Json(new { Success = false, Result = T("Newsletter.Email.Wrong").Value });
        }

        email = email.Trim();

        if (subscribe)
        {
            await _newsletterSubscriptionService.SubscribeAsync(email, customer);
        }
        else
        {
            await _newsletterSubscriptionService.UnsubscribeAsync(email, false);
        }

        return Json(new
        {
            Success = true,
            Result = T(subscribe ? "Newsletter.SubscribeEmailSent" : "Newsletter.UnsubscribeEmailSent").Value
        });
    }

    [HttpGet]
    [DisallowRobot]
    [LocalizedRoute("/newsletter/subscriptionactivation/{token}/{active}", Name = "NewsletterActivation")]
    public async Task<IActionResult> SubscriptionActivation(Guid token, bool active)
    {
        if (!await _newsletterSubscriptionService.ActivateAsync(token, active))
        {
            return NotFound();
        }

        var model = new SubscriptionActivationModel
        {
            Result = T(active ? "Newsletter.ResultActivated" : "Newsletter.ResultDeactivated")
        };

        return View(model);
    }
}