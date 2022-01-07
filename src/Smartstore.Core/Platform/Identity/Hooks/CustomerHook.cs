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
				if (customer.Deleted && customer.IsSystemAccount)
				{
					_hookErrorMessage = $"System customer account '{customer.SystemName}' cannot be deleted.";
				}
				else if (entry.InitialState == EState.Added)
				{
					if (customer.Email.HasValue() && await _db.Customers.AsQueryable().AnyAsync(x => x.Email == customer.Email, cancelToken))
					{
						_hookErrorMessage = T("Identity.Error.DuplicateEmail", customer.Email);
					}
					else if (customer.Username.HasValue() &&
						_customerSettings.CustomerLoginType != CustomerLoginType.Email &&
						await _db.Customers.AsQueryable().AnyAsync(x => x.Username == customer.Username, cancelToken))
					{
						_hookErrorMessage = T("Identity.Error.DuplicateUserName", customer.Username);
					}
					else
					{
						UpdateFullName(customer);
					}
				}
				else if (entry.InitialState == EState.Modified)
				{
					UpdateFullName(customer);
				}

				if (_hookErrorMessage.HasValue())
				{
					return RevertChanges(entry);
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

		private static HookResult RevertChanges(IHookedEntity entry)
		{
			if (entry.State == EState.Modified)
			{
				entry.State = EState.Unchanged;
			}
			else if (entry.State == EState.Added)
			{
				entry.State = EState.Detached;
			}

			// We need to return HookResult.Ok instead of HookResult.Failed to be able to output an error notification.
			return HookResult.Ok;
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
