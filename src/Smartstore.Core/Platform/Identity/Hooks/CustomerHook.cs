using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Data.Hooks;
using EState = Smartstore.Data.EntityState;

namespace Smartstore.Core.Identity
{
    [Important]
    internal class CustomerHook : AsyncDbSaveHook<Customer>
    {
        private static readonly string[] _candidateProps =
        [
            nameof(Customer.Title),
            nameof(Customer.Salutation),
            nameof(Customer.FirstName),
            nameof(Customer.LastName)
        ];

        private readonly SmartDbContext _db;
        private readonly Lazy<IMessageFactory> _messageFactory;
        private readonly CustomerSettings _customerSettings;
        private string _hookErrorMessage;

        // Key: old email. Value: new email.
        private readonly Dictionary<string, string> _modifiedEmails = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _emailsToUnsubscribe = new(StringComparer.OrdinalIgnoreCase);

        public CustomerHook(
            SmartDbContext db,
            Lazy<IMessageFactory> messageFactory,
            CustomerSettings customerSettings)
        {
            _db = db;
            _messageFactory = messageFactory;
            _customerSettings = customerSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public override async Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.Entity is Customer customer)
            {
                if (ValidateCustomer(customer))
                {
                    if (entry.InitialState == EState.Added || entry.InitialState == EState.Modified)
                    {
                        UpdateFullName(customer);

                        if (_customerSettings.UserRegistrationType == UserRegistrationType.AdminApproval
                            && entry.Entry.TryGetModifiedProperty(nameof(customer.Active), out var prevValue)
                            && !(bool)prevValue
                            && customer.Active)
                        {
                            await _messageFactory.Value.SendCustomerWelcomeMessageAsync(customer, customer.GenericAttributes.LanguageId ?? 0);
                        }

                        if (entry.Entry.TryGetModifiedProperty(nameof(customer.Email), out var prevEmail)
                            && prevEmail != null
                            && customer.Email != null)
                        {
                            var newEmail = customer.Email.EmptyNull().Trim().Truncate(255);
                            if (newEmail.IsEmail())
                            {
                                _modifiedEmails[prevEmail.ToString()] = newEmail;
                            }
                        }
                    }
                }
                else
                {
                    entry.ResetState();
                }
            }

            return HookResult.Ok;
        }

        public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_hookErrorMessage.HasValue())
            {
                var message = new string(_hookErrorMessage);
                _hookErrorMessage = null;

                throw new HookException(message);
            }

            return Task.CompletedTask;
        }

        protected override Task<HookResult> OnUpdatedAsync(Customer entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.IsSoftDeleted == true && entity.Email.HasValue())
            {
                _emailsToUnsubscribe.Add(entity.Email);
            }

            return Task.FromResult(HookResult.Ok);
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_modifiedEmails.Count > 0)
            {
                // Update newsletter subscription if email changed.
                var oldEmails = _modifiedEmails.Keys.ToArray();

                foreach (var oldEmailsChunk in oldEmails.Chunk(100))
                {
                    var subscriptions = await _db.NewsletterSubscriptions
                        .Where(x => oldEmailsChunk.Contains(x.Email))
                        .ToListAsync(cancelToken);

                    // INFO: we do not use BatchUpdateAsync because of NewsletterSubscription hook.
                    subscriptions.Each(x => x.Email = _modifiedEmails[x.Email]);

                    await _db.SaveChangesAsync(cancelToken);
                }

                _modifiedEmails.Clear();
            }

            // Unsubscribe from newsletter if customer was soft-deleted.
            foreach (var chunk in _emailsToUnsubscribe.Chunk(50))
            {
                await _db.NewsletterSubscriptions
                    .Where(x => chunk.Contains(x.Email))
                    .ExecuteDeleteAsync(cancelToken);
            }

            _emailsToUnsubscribe.Clear();
        }

        private bool ValidateCustomer(Customer customer)
        {
            // INFO: do not validate email and username here. UserValidator is responsible for this.
            if (customer.Deleted && customer.IsSystemAccount)
            {
                _hookErrorMessage = $"System customer account '{customer.SystemName}' cannot be deleted.";
                return false;
            }

            return true;
        }

        private void UpdateFullName(Customer entity)
        {
            var shouldUpdate = entity.IsTransientRecord();

            if (!shouldUpdate)
            {
                shouldUpdate = entity.FullName.IsEmpty() && (entity.FirstName.HasValue() || entity.LastName.HasValue());
            }

            if (!shouldUpdate)
            {
                var modProps = _db.GetModifiedProperties(entity);
                shouldUpdate = _candidateProps.Any(modProps.ContainsKey);
            }

            if (shouldUpdate)
            {
                var parts = new[]
                {
                    entity.Salutation,
                    entity.Title,
                    entity.FirstName,
                    entity.LastName
                };

                entity.FullName = string.Join(" ", parts.Where(x => x.HasValue())).NullEmpty();
            }
        }
    }
}
