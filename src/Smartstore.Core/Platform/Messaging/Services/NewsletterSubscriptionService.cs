using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Events;
using EState = Smartstore.Data.EntityState;

namespace Smartstore.Core.Messaging
{
    public class NewsletterSubscriptionService : AsyncDbSaveHook<NewsletterSubscription>, INewsletterSubscriptionService
    {
        private readonly HashSet<NewsletterSubscription> _toSubscribe = new();
        private readonly HashSet<NewsletterSubscription> _toUnsubscribe = new();

        private readonly SmartDbContext _db;
        private readonly IEventPublisher _eventPublisher;
        private readonly IWorkContext _workContext;
        private readonly IMessageFactory _messageFactory;

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

        protected override Task<HookResult> OnUpdatingAsync(NewsletterSubscription entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            EnsureValidEntityOrThrow(entity, entry);

            return Task.FromResult(HookResult.Ok);
        }

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.Entity is NewsletterSubscription subscription)
            {
                if (entry.State == EState.Modified)
                {
                    var modProps = _db.GetModifiedProperties(subscription);
                    var origActive = modProps.TryGetValue(nameof(NewsletterSubscription.Active), out object active) ? (bool)active : subscription.Active;
                    var origEmail = modProps.TryGetValue(nameof(NewsletterSubscription.Email), out object email) ? email.ToString() : subscription.Email;

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
                else if (subscription.Active)
                {
                    if (entry.State == EState.Added)
                    {
                        _toSubscribe.Add(subscription);
                    }
                    else if (entry.State == EState.Deleted)
                    {
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
            // Subscriptions without store IDs aren't allowed.
            if (entity.StoreId == 0)
            {
                entry.State = EState.Detached;
                throw new InvalidOperationException("Newsletter subscription must be assigned to a valid store.");
            }

            // Format and validate mail address.
            string email = entity.Email.EmptyNull().Trim().Truncate(255);

            if (!email.IsEmail())
            {
                entry.State = EState.Detached;
                throw new InvalidOperationException("Newsletter subscription must be assigned to a valid email address.");
            }

            entity.Email = email;
        }

        #endregion

        public virtual bool Subscribe(NewsletterSubscription subscription)
        {
            Guard.NotNull(subscription);

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
            Guard.NotNull(subscription);

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

            var language = _workContext.WorkingLanguage;
            var subscription = await _db.NewsletterSubscriptions
                .ApplyMailAddressFilter(email, storeId)
                .FirstOrDefaultAsync();

            if (subscription != null)
            {
                if (subscribe)
                {
                    if (!subscription.Active)
                    {
                        await _messageFactory.SendNewsletterSubscriptionActivationMessageAsync(subscription, language.Id);
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
                        WorkingLanguageId = language.Id
                    };

                    _db.NewsletterSubscriptions.Add(subscription);

                    await _messageFactory.SendNewsletterSubscriptionActivationMessageAsync(subscription, language.Id);

                    result = true;
                }
            }

            return result;
        }
    }
}
