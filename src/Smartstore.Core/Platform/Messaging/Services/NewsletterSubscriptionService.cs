using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Events;

namespace Smartstore.Core.Messages
{
    public class NewsletterSubscriptionService : AsyncDbSaveHook<NewsletterSubscription>, INewsletterSubscriptionService
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

        protected override Task<HookResult> OnInsertingAsync(NewsletterSubscription entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            Guard.NotNull(entity, nameof(entity));

            if (entity.StoreId == 0)
                throw new SmartException("Newsletter subscription must be assigned to a valid store.");

            // Format and validate mail address.
            entity.Email = EnsureSubscriberEmailOrThrow(entity.Email);

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnUpdatingAsync(NewsletterSubscription entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            Guard.NotNull(entity, nameof(entity));

            if (entity.StoreId == 0)
                throw new SmartException("Newsletter subscription must be assigned to a valid store.");

            // Format and validate mail address.
            entity.Email = EnsureSubscriberEmailOrThrow(entity.Email);

            return Task.FromResult(HookResult.Ok);
        }

        protected override async Task<HookResult> OnDeletedAsync(NewsletterSubscription entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            Guard.NotNull(entity, nameof(entity));

            await _eventPublisher.PublishNewsletterUnsubscribedAsync(entity.Email);
            return HookResult.Ok;
        }

        #endregion

        public virtual async Task<bool> SubscribeAsync(NewsletterSubscription subscription)
        {
            Guard.NotNull(subscription, nameof(subscription));

            if (!subscription.Active)
            {
                // Ensure that entity is tracked
                _db.TryChangeState(subscription, EntityState.Modified);

                subscription.Active = true;
                await _db.SaveChangesAsync();

                // Publish the unsubscription event.
                await _eventPublisher.PublishNewsletterSubscribedAsync(subscription.Email);
                return true;
            }

            return false;
        }
        
        public virtual async Task<bool> UnsubscribeAsync(NewsletterSubscription subscription)
        {
            Guard.NotNull(subscription, nameof(subscription));

            if (subscription.Active)
            {
                _db.TryChangeState(subscription, EntityState.Modified);

                subscription.Active = false;
                await _db.SaveChangesAsync();

                // Publish the unsubscription event.
                await _eventPublisher.PublishNewsletterUnsubscribedAsync(subscription.Email);
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
            var origActive = modProps.ContainsKey(nameof(NewsletterSubscription.Active)) ? (bool)modProps[nameof(NewsletterSubscription.Active)] : subscription.Active;
            var origEmail = modProps.ContainsKey(nameof(NewsletterSubscription.Email)) ? modProps[nameof(NewsletterSubscription.Email)].ToString() : subscription.Email;

            // Persist.
            await _db.SaveChangesAsync();

            // Publish events.
            if ((origActive == false && subscription.Active) || (subscription.Active && (origEmail != subscription.Email)))
            {
                // If the original entry was false and and the current is true
                // or the mail address changed and subscription is active
                // > publish subscribed.
                await _eventPublisher.PublishNewsletterSubscribedAsync(subscription.Email);
                subscribed = true;
            }
            else if ((origActive && subscription.Active && (origEmail != subscription.Email)) || (origActive && !subscription.Active))
            {
                // If the two mail adresses are different > publish unsubscribed.
                // or if the original entry was true and the current is false
                // > publish unsubscribed.
                await _eventPublisher.PublishNewsletterUnsubscribedAsync(subscription.Email);
                subscribed = false;
            }
            
            return subscribed;
        }

        public virtual async Task<bool?> ApplySubscriptionAsync(bool subscribe, string email, int storeId)
        {
            bool? result = null;

            if (email.IsEmail())
            {
                var subscription = await _db.NewsletterSubscriptions
                    .ApplyMailAddressFilter(email, storeId)
                    .FirstOrDefaultAsync();

                if (subscription != null)
                {
                    if (subscribe)
                    {
                        if (!subscription.Active)
                        {
                            // TODO: (mh) (core) make async
                            // _messageFactory.SendNewsLetterSubscriptionActivationMessage(subscription, _workContext.WorkingLanguage.Id);
                        }

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
                    if (subscribe)
                    {
                        subscription = new NewsletterSubscription
                        {
                            NewsletterSubscriptionGuid = Guid.NewGuid(),
                            Email = email,
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
