using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Data.Hooks;
using EState = Smartstore.Data.EntityState;

namespace Smartstore.Core.Identity
{
    [Important]
    internal class CustomerHook : AsyncDbSaveHook<Customer>
    {
        private static readonly string[] _candidateProps = new[]
        {
            nameof(Customer.Title),
            nameof(Customer.Salutation),
            nameof(Customer.FirstName),
            nameof(Customer.LastName)
        };

        private readonly SmartDbContext _db;
        private string _hookErrorMessage;

        // Key: old email. Value: new email.
        private readonly Dictionary<string, string> _modifiedEmails = new(StringComparer.OrdinalIgnoreCase);

        public CustomerHook(SmartDbContext db)
        {
            _db = db;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public override Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.Entity is Customer customer)
            {
                if (ValidateCustomer(customer))
                {
                    if (entry.InitialState == EState.Added || entry.InitialState == EState.Modified)
                    {
                        UpdateFullName(customer);

                        if (entry.Entry.TryGetModifiedProperty(nameof(customer.Email), out var originalValue)
                            && originalValue != null
                            && customer.Email != null)
                        {
                            var oldEmail = originalValue.ToString();
                            var newEmail = customer.Email.EmptyNull().Trim().Truncate(255);

                            if (newEmail.IsEmail())
                            {
                                _modifiedEmails[oldEmail] = newEmail;
                            }
                        }
                    }
                }
                else
                {
                    entry.ResetState();
                }
            }

            return Task.FromResult(HookResult.Ok);
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
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Update newsletter subscription if email changed.
            if (_modifiedEmails.Any())
            {
                var oldEmails = _modifiedEmails.Keys.ToArray();

                foreach (var oldEmailsChunk in oldEmails.Chunk(50))
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
                shouldUpdate = _candidateProps.Any(x => modProps.ContainsKey(x));
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
