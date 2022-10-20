using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Events;

namespace Smartstore.Core.Messaging
{
    public class NewsletterSubscriptionService : AsyncDbSaveHook<NewsletterSubscription>, INewsletterSubscriptionService
    {
        private readonly SmartDbContext _db;
        private readonly IEventPublisher _eventPublisher;
        private readonly IWorkContext _workContext;
        private readonly IMessageFactory _messageFactory;
        private readonly HashSet<NewsletterSubscription> _toSubscribe = new();
        private readonly HashSet<NewsletterSubscription> _toUnsubscribe = new();

        public NewsletterSubscriptionService(
            SmartDbContext db,
            IEventPublisher eventPublisher,
            IWorkContext workContext,
            IMessageFactory messageFactory)
        {
            _db = db;
            _eventPublisher = eventPublisher;
            _workContext = workContext;
            _messageFactory = messageFactory;
        }

        #region Hook 

        protected override Task<HookResult> OnInsertingAsync(NewsletterSubscription entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            EnsureValidEntityOrThrow(entity, entry);
            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnInsertedAsync(NewsletterSubscription entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entity.Active)
            {
                _toSubscribe.Add(entity);
            }

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnUpdatingAsync(NewsletterSubscription entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            EnsureValidEntityOrThrow(entity, entry);

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnDeletedAsync(NewsletterSubscription entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            foreach (var entry in entries)
            {
                var subscription = (NewsletterSubscription)entry.Entity;

                if (entry.State == Smartstore.Data.EntityState.Deleted && subscription.Active)
                {
                    _toUnsubscribe.Add(subscription);
                }
                else if (entry.State == Smartstore.Data.EntityState.Modified)
                {
                    var modProps = _db.GetModifiedProperties(subscription);
                    var origActive = modProps.ContainsKey(nameof(NewsletterSubscription.Active)) ? (bool)modProps[nameof(NewsletterSubscription.Active)] : subscription.Active;
                    var origEmail = modProps.ContainsKey(nameof(NewsletterSubscription.Email)) ? modProps[nameof(NewsletterSubscription.Email)].ToString() : subscription.Email;

                    // Collect events for modified entities.
                    if ((origActive == false && subscription.Active) || (subscription.Active && (origEmail != subscription.Email)))
                    {
                        // If the original entry was false and and the current is true
                        // or the mail address changed and subscription is active
                        // > publish subscribed.
                        _toSubscribe.Add(subscription);
                    }
                    else if ((origActive && subscription.Active && (origEmail != subscription.Email)) || (origActive && !subscription.Active))
                    {
                        // If the two mail adresses are different > publish unsubscribed.
                        // or if the original entry was true and the current is false
                        // > publish unsubscribed.
                        _toUnsubscribe.Add(subscription);
                    }
                }
            }

            return Task.FromResult(HookResult.Ok);
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            foreach (var subscription in _toSubscribe)
            {
                await _eventPublisher.PublishNewsletterSubscribedAsync(subscription.Email);
            }
            _toSubscribe.Clear();

            foreach (var subscription in _toUnsubscribe)
            {
                await _eventPublisher.PublishNewsletterUnsubscribedAsync(subscription.Email);
            }
            _toUnsubscribe.Clear();
        }

        private static void EnsureValidEntityOrThrow(NewsletterSubscription entity, IHookedEntity entry)
        {
            // Subscriptions without store ids aren't allowed.
            if (entity.StoreId == 0)
            {
                entry.State = Smartstore.Data.EntityState.Detached;
                throw new InvalidOperationException("Newsletter subscription must be assigned to a valid store.");
            }

            // Format and validate mail address.
            string email = entity.Email.EmptyNull().Trim().Truncate(255);

            if (!email.IsEmail())
            {
                entry.State = Smartstore.Data.EntityState.Detached;
                throw new InvalidOperationException("Newsletter subscription must be assigned to a valid email address.");
            }

            entity.Email = email;
        }

        #endregion

        public virtual bool Subscribe(NewsletterSubscription subscription)
        {
            Guard.NotNull(subscription, nameof(subscription));

            if (!subscription.Active)
            {
                // Ensure that entity is tracked.
                _db.TryUpdate(subscription);

                subscription.Active = true;
                return true;
            }

            return false;
        }

        public virtual bool Unsubscribe(NewsletterSubscription subscription)
        {
            Guard.NotNull(subscription, nameof(subscription));

            if (subscription.Active)
            {
                // Ensure that entity is tracked.
                _db.TryUpdate(subscription);

                subscription.Active = false;
                return true;
            }

            return false;
        }

        public virtual async Task<bool?> ApplySubscriptionAsync(bool subscribe, string email, int storeId)
        {
            bool? result = null;

            if (!email.IsEmail())
            {
                throw new ArgumentException("Email parameter must be a valid email address.", nameof(email));
            }

            var subscription = await _db.NewsletterSubscriptions
                .ApplyMailAddressFilter(email, storeId)
                .FirstOrDefaultAsync();

            if (subscription != null)
            {
                if (subscribe)
                {
                    if (!subscription.Active)
                    {
                        await _messageFactory.SendNewsletterSubscriptionActivationMessageAsync(subscription, _workContext.WorkingLanguage.Id);
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

                    await _messageFactory.SendNewsletterSubscriptionActivationMessageAsync(subscription, _workContext.WorkingLanguage.Id);

                    result = true;
                }
            }

            return result;
        }
    }
}
