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
		private readonly CustomerSettings _customerSettings;
		private string _hookErrorMessage;

		public CustomerHook(SmartDbContext db, CustomerSettings customerSettings)
        {
			_db = db;
			_customerSettings = customerSettings;
        }

		public Localizer T { get; set; } = NullLocalizer.Instance;

		public override async Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
		{
			if (entry.Entity is Customer customer)
			{
                if (await ValidateCustomer(customer, entry, cancelToken))
                {
                    if (entry.InitialState == EState.Added || entry.InitialState == EState.Modified)
                    {
                        UpdateFullName(customer);
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

				throw new SmartException(message);
			}

			return Task.CompletedTask;
		}

        // TODO: (mg) (core) update newsletter subscription email if changed.

        private async Task<bool> ValidateCustomer(Customer customer, IHookedEntity entry, CancellationToken cancelToken)
        {
            // INFO: do not validate email and username here. UserValidator is responsible for this.
            if (customer.Deleted && customer.IsSystemAccount)
            {
                _hookErrorMessage = $"System customer account '{customer.SystemName}' cannot be deleted.";
                return false;
            }

            if (!await ValidateCustomerNumber(customer, cancelToken))
            {
                return false;
            }

            return true;
        }

        private async Task<bool> ValidateCustomerNumber(Customer customer, CancellationToken cancelToken)
        {
            if (customer.CustomerNumber.HasValue() && _customerSettings.CustomerNumberMethod != CustomerNumberMethod.Disabled)
            {
                var customerNumberExists = await _db.Customers
                    .IgnoreQueryFilters()
                    .AnyAsync(x => x.CustomerNumber == customer.CustomerNumber && (customer.Id == 0 || customer.Id != x.Id), cancelToken);

                if (customerNumberExists)
                {
                    _hookErrorMessage = T("Common.CustomerNumberAlreadyExists");
                    return false;
                }
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
