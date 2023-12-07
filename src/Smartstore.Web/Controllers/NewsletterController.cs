using Smartstore.Core.Identity;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Messaging;
using Smartstore.Core.Seo.Routing;
using Smartstore.Core.Stores;
using Smartstore.Web.Models.Newsletter;

namespace Smartstore.Web.Controllers
{
    public class NewsletterController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IMessageFactory _messageFactory;
        private readonly PrivacySettings _privacySettings;

        public NewsletterController(
            SmartDbContext db,
            IWorkContext workContext,
            IMessageFactory messageFactory,
            IStoreContext storeContext,
            PrivacySettings privacySettings)
        {
            _db = db;
            _workContext = workContext;
            _messageFactory = messageFactory;
            _storeContext = storeContext;
            _privacySettings = privacySettings;
        }

        [HttpPost]
        [GdprConsent, DisallowRobot]
        [LocalizedRoute("newsletter/subscribe", Name = "SubscribeNewsletter")]
        public async Task<IActionResult> Subscribe(bool subscribe, string email)
        {
            string result;
            var success = false;
            var customer = _workContext.CurrentCustomer;
            var hasConsentedToGdpr = customer.GenericAttributes.HasConsentedToGdpr;
            var hasConsented = ViewData["GdprConsent"] != null ? (bool)ViewData["GdprConsent"] : hasConsentedToGdpr;

            if (!hasConsented && _privacySettings.DisplayGdprConsentOnForms)
            {
                return Json(new
                {
                    Success = success,
                    Result = string.Empty
                });
            }

            if (!email.IsEmail())
            {
                result = T("Newsletter.Email.Wrong");
            }
            else
            {
                // subscribe/unsubscribe
                email = email.Trim();

                var subscription = await _db.NewsletterSubscriptions
                    .AsNoTracking()
                    .ApplyMailAddressFilter(email, _storeContext.CurrentStore.Id)
                    .FirstOrDefaultAsync();

                if (subscription != null)
                {
                    if (subscribe)
                    {
                        if (!subscription.Active)
                        {
                            await _messageFactory.SendNewsletterSubscriptionActivationMessageAsync(subscription, _workContext.WorkingLanguage.Id);
                        }
                        result = T("Newsletter.SubscribeEmailSent");
                    }
                    else
                    {
                        if (subscription.Active)
                        {
                            await _messageFactory.SendNewsletterSubscriptionDeactivationMessageAsync(subscription, _workContext.WorkingLanguage.Id);
                        }
                        result = T("Newsletter.UnsubscribeEmailSent");
                    }
                }
                else if (subscribe)
                {
                    subscription = new NewsletterSubscription
                    {
                        NewsletterSubscriptionGuid = Guid.NewGuid(),
                        Email = email,
                        Active = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        StoreId = _storeContext.CurrentStore.Id,
                        WorkingLanguageId = _workContext.WorkingLanguage.Id
                    };

                    _db.NewsletterSubscriptions.Add(subscription);
                    await _db.SaveChangesAsync();

                    await _messageFactory.SendNewsletterSubscriptionActivationMessageAsync(subscription, _workContext.WorkingLanguage.Id);

                    result = T("Newsletter.SubscribeEmailSent");
                }
                else
                {
                    result = T("Newsletter.UnsubscribeEmailSent");
                }

                success = true;
            }

            return Json(new
            {
                Success = success,
                Result = result
            });
        }

        [HttpGet]
        [DisallowRobot]
        [LocalizedRoute("/newsletter/subscriptionactivation/{token}/{active}", Name = "NewsletterActivation")]
        public async Task<IActionResult> SubscriptionActivation(Guid token, bool active)
        {
            var subscription = await _db.NewsletterSubscriptions
                .FirstOrDefaultAsync(x => x.NewsletterSubscriptionGuid == token);

            if (subscription == null)
            {
                return NotFound();
            }

            var model = new SubscriptionActivationModel();

            if (active)
            {
                subscription.Active = active;
            }
            else
            {
                _db.NewsletterSubscriptions.Remove(subscription);
            }

            await _db.SaveChangesAsync();

            model.Result = T(active ? "Newsletter.ResultActivated" : "Newsletter.ResultDeactivated");

            return View(model);
        }
    }
}