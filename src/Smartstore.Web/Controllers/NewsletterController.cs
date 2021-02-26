using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Messages;
using Smartstore.Core.Stores;
using Smartstore.Web.Filters;
using Smartstore.Web.Models.Newsletter;

namespace Smartstore.Web.Controllers
{
    public class NewsletterController : SmartController
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
        [GdprConsent]
        public async Task<ActionResult> SubscribeAsync(bool subscribe, string email)
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
                            await _messageFactory.SendNewsLetterSubscriptionActivationMessageAsync(subscription, _workContext.WorkingLanguage.Id);
                        }
                        result = T("Newsletter.SubscribeEmailSent");
                    }
                    else
                    {
                        if (subscription.Active)
                        {
                            await _messageFactory.SendNewsLetterSubscriptionDeactivationMessageAsync(subscription, _workContext.WorkingLanguage.Id);
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

                    await _messageFactory.SendNewsLetterSubscriptionActivationMessageAsync(subscription, _workContext.WorkingLanguage.Id);

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
        [LocalizedRoute("/newsletter/subscriptionactivation", Name = "SubscriptionActivation")]
        public async Task<ActionResult> SubscriptionActivationAsync(Guid token, bool active)
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