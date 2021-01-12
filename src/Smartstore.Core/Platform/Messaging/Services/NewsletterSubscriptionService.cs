using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Events;

namespace Smartstore.Core.Messages
{
    public class NewsletterSubscriptionService : DbSaveHook<NewsletterSubscription>, INewsletterSubscriptionService
    {
        private readonly SmartDbContext _db;
        private readonly IEventPublisher _eventPublisher;
        private readonly IWorkContext _workContext;

        // TODO: (mh) (core) Comment in once MessageFactory is available.
        //private readonly IMessageFactory _messageFactory;

        public NewsletterSubscriptionService(
            SmartDbContext db,
            IEventPublisher eventPublisher,
            IWorkContext workContext)
            //,IMessageFactory messageFactory)
        {
            _db = db;
            _eventPublisher = eventPublisher;
            _workContext = workContext;
            //_messageFactory = messageFactory;
        }

        #region Hook 

        protected override HookResult OnInserting(NewsletterSubscription entity, IHookedEntity entry)
        {
            Guard.NotNull(entity, nameof(entity));

            if (entity.StoreId == 0)
                throw new SmartException("Newsletter subscription must be assigned to a valid store.");

            // Format and validate mail address.
            entity.Email = EnsureSubscriberEmailOrThrow(entity.Email);

            return HookResult.Ok;
        }

        protected override HookResult OnUpdating(NewsletterSubscription entity, IHookedEntity entry)
        {
            Guard.NotNull(entity, nameof(entity));

            if (entity.StoreId == 0)
                throw new SmartException("Newsletter subscription must be assigned to a valid store.");

            // Format and validate mail address.
            entity.Email = EnsureSubscriberEmailOrThrow(entity.Email);

            return HookResult.Ok;
        }

        protected override HookResult OnDeleted(NewsletterSubscription entity, IHookedEntity entry)
        {
            Guard.NotNull(entity, nameof(entity));

            _eventPublisher.PublishNewsletterUnsubscribed(entity.Email);
        
            return HookResult.Ok;
        }

        #endregion

        public virtual async Task<bool> SubscribeAsnyc(NewsletterSubscription subscription)
        {
            Guard.NotNull(subscription, nameof(subscription));

            if (!subscription.Active)
            {
                subscription.Active = true;

                _db.NewsletterSubscriptions.Update(subscription);
                await _db.SaveChangesAsync();

                // Publish the unsubscription event.
                await _eventPublisher.PublishNewsletterSubscribed(subscription.Email);
                return true;
            }

            return false;
        }
        
        public virtual async Task<bool> UnsubscribeAsync(NewsletterSubscription subscription)
        {
            Guard.NotNull(subscription, nameof(subscription));

            if (subscription.Active)
            {
                subscription.Active = false;

                _db.NewsletterSubscriptions.Update(subscription);
                await _db.SaveChangesAsync();

                // Publish the unsubscription event.
                await _eventPublisher.PublishNewsletterUnsubscribed(subscription.Email);
                return true;
            }

            return false;
        }

        public virtual async Task<bool?> UpdateSubscriptionAsync(NewsletterSubscription subscription)
        {
            Guard.NotNull(subscription, nameof(subscription));

            bool? subscribed = null;

            // Get original values of subscription entity.
            var modProps = _db.GetModifiedProperties(subscription);
            var origActive = modProps.ContainsKey("CategoryId") ? (bool)modProps["Active"] : subscription.Active;
            var origEmail = modProps.ContainsKey("Email") ? modProps["Email"].ToString() : subscription.Email;

            // Persist.
            _db.NewsletterSubscriptions.Update(subscription);
            await _db.SaveChangesAsync();

            // Publish events.
            if ((origActive == false && subscription.Active) || (subscription.Active && (origEmail != subscription.Email)))
            {
                // If the original entry was false and and the current is true
                // or the mail address changed and subscription is active
                // > publish subscribed.
                await _eventPublisher.PublishNewsletterSubscribed(subscription.Email);
                subscribed = true;
            }
            else if ((origActive && subscription.Active && (origEmail != subscription.Email)) || (origActive && !subscription.Active))
            {
                // If the two mail adresses are different > publish unsubscribed.
                // or if the original entry was true and the current is false
                // > publish unsubscribed.
                await _eventPublisher.PublishNewsletterUnsubscribed(subscription.Email);
                subscribed = false;
            }
            
            return subscribed;
        }

        public virtual async Task<bool?> AddNewsletterSubscriptionAsync(bool add, string mail, int storeId)
        {
            bool? result = null;

            if (mail.IsEmail())
            {
                var subscription = await _db.NewsletterSubscriptions
                    .ApplyMailAddressFilter(mail, storeId)
                    .FirstOrDefaultAsync();

                if (subscription != null)
                {
                    if (add)
                    {
                        if (!subscription.Active)
                        {
                            // TODO: (mh) (core) make async
                            // _messageFactory.SendNewsLetterSubscriptionActivationMessage(subscription, _workContext.WorkingLanguage.Id);
                        }

                        // TODO: (mh) (core) why update? nothing has happened.
                        _db.NewsletterSubscriptions.Update(subscription);
                        result = true;
                    }
                    else
                    {
                        _db.NewsletterSubscriptions.Remove(subscription);
                        result = false;
                    }
                }
                else
                {
                    if (add)
                    {
                        subscription = new NewsletterSubscription
                        {
                            NewsletterSubscriptionGuid = Guid.NewGuid(),
                            Email = mail,
                            Active = false,
                            CreatedOnUtc = DateTime.UtcNow,
                            StoreId = storeId,
                            WorkingLanguageId = _workContext.WorkingLanguage.Id
                        };

                        _db.NewsletterSubscriptions.Add(subscription);

                        // TODO: (mh) (core) make async
                        //_messageFactory.SendNewsLetterSubscriptionActivationMessage(subscription, _workContext.WorkingLanguage.Id);

                        result = true;
                    }
                }

                await _db.SaveChangesAsync();
            }

            return result;
        }

        private static string EnsureSubscriberEmailOrThrow(string email)
        {
            // TODO: (mh) (core) Maybe this validation should better be placed in the frontend/backend models.

            string output = email.EmptyNull().Trim().Truncate(255);

            if (!output.IsEmail())
            {
                throw Error.ArgumentOutOfRange("email", "Email is not valid.", email);
            }

            return output;
        }
    }
}
