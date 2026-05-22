using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Data.Hooks;
using Smartstore.Events;
using EState = Smartstore.Data.EntityState;

namespace Smartstore.Core.Messaging;

public class NewsletterSubscriptionService : AsyncDbSaveHook<NewsletterSubscription>, INewsletterSubscriptionService
{
    private readonly HashSet<NewsletterSubscription> _toSubscribe = [];
    private readonly HashSet<NewsletterSubscription> _toUnsubscribe = [];

    private readonly SmartDbContext _db;
    private readonly IWorkContext _workContext;
    private readonly IStoreContext _storeContext;
    private readonly IEventPublisher _eventPublisher;
    private readonly IMessageFactory _messageFactory;
    private readonly ICustomerService _customerService;

    public NewsletterSubscriptionService(
        SmartDbContext db,
        IWorkContext workContext,
        IStoreContext storeContext,
        IEventPublisher eventPublisher,
        IMessageFactory messageFactory,
        ICustomerService customerService)
    {
        _db = db;
        _workContext = workContext;
        _storeContext = storeContext;
        _eventPublisher = eventPublisher;
        _messageFactory = messageFactory;
        _customerService = customerService;
    }

    public Localizer T { get; set; } = NullLocalizer.Instance;

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

    public virtual async Task<bool> SubscribeAsync(
        string email,
        Customer customer = null,
        bool active = false,
        int? storeId = null)
    {
        if (!email.IsEmail())
        {
            return false;
        }

        customer ??= _workContext.CurrentCustomer;
        storeId ??= _storeContext.CurrentStore.Id;

        var language = _workContext.WorkingLanguage;
        var subscription = await _db.NewsletterSubscriptions
            .ApplyTracking(active)
            .ApplyMailAddressFilter(email, storeId.Value)
            .FirstOrDefaultAsync();

        if (subscription != null)
        {
            if (!subscription.Active)
            {
                if (active)
                {
                    subscription.Active = true;

                    _customerService.ApplyRewardPointsForNewsletterSubscription(customer);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    await _messageFactory.SendNewsletterSubscriptionActivationMessageAsync(subscription, language.Id);
                }
            }
        }
        else
        {
            subscription = new()
            {
                NewsletterSubscriptionGuid = Guid.NewGuid(),
                Email = email,
                Active = active,
                CreatedOnUtc = DateTime.UtcNow,
                StoreId = storeId.Value,
                WorkingLanguageId = language.Id
            };

            _db.NewsletterSubscriptions.Add(subscription);

            if (active)
            {
                _customerService.ApplyRewardPointsForNewsletterSubscription(customer);
                await _db.SaveChangesAsync();
            }
            else
            {
                await _db.SaveChangesAsync();
                await _messageFactory.SendNewsletterSubscriptionActivationMessageAsync(subscription, language.Id);
            }
        }

        return true;
    }

    public virtual async Task<bool> UnsubscribeAsync(
        string email,
        bool remove = true,
        int? storeId = null)
    {
        if (!email.IsEmail())
        {
            return false;
        }

        var subscription = await _db.NewsletterSubscriptions
            .ApplyTracking(remove)
            .ApplyMailAddressFilter(email, storeId ?? _storeContext.CurrentStore.Id)
            .FirstOrDefaultAsync();
        if (subscription == null)
        {
            return false;
        }

        if (remove)
        {
            _db.NewsletterSubscriptions.Remove(subscription);
            // INFO: Better not to reduce reward points when a customer cancels their subscription.
            // This could lead to frustration, raises some legal concerns, and is not in line with standard practice.

            await _db.SaveChangesAsync();
        }
        else if (subscription.Active)
        {
            await _messageFactory.SendNewsletterSubscriptionDeactivationMessageAsync(subscription, _workContext.WorkingLanguage.Id);
        }

        return true;
    }

    public virtual async Task<bool> ActivateAsync(Guid token, bool active)
    {
        if (token == Guid.Empty)
        {
            return false;
        }

        var customerQuery = _db.Customers
            .AsSplitQuery()
            .IncludeCustomerRoles()
            .Include(x => x.RewardPointsHistory);

        var query =
            from ns in _db.NewsletterSubscriptions
            join c in customerQuery on ns.Email equals c.Email into customers
            from c in customers.DefaultIfEmpty()
            where ns.NewsletterSubscriptionGuid == token
            select new NewsletterSubscriber
            {
                Subscription = ns,
                Customer = c
            };

        var subscriber = await query.FirstOrDefaultAsync();

        if (subscriber?.Subscription == null)
        {
            return false;
        }

        if (active)
        {
            subscriber.Subscription.Active = true;

            if (subscriber.Customer != null)
            {
                _customerService.ApplyRewardPointsForNewsletterSubscription(subscriber.Customer);
            }
        }
        else
        {
            _db.NewsletterSubscriptions.Remove(subscriber.Subscription);
        }

        await _db.SaveChangesAsync();
        return true;
    }
}