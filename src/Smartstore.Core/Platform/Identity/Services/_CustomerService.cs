using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Collections;
using Smartstore.Core.Data;
using Smartstore.Core.Web;
using Smartstore.Data.Caching;
using Smartstore.Data.Hooks;
using Smartstore.Diagnostics;

namespace Smartstore.Core.Identity
{
    public partial class CustomerService : AsyncDbSaveHook<Customer>, ICustomerService
    {
		#region Raw SQL
		const string SqlGenericAttributes = @"
DELETE TOP(50000) [g]
  FROM [dbo].[GenericAttribute] AS [g]
  LEFT OUTER JOIN [dbo].[Customer] AS [c] ON c.Id = g.EntityId
  LEFT OUTER JOIN [dbo].[Order] AS [o] ON c.Id = o.CustomerId
  LEFT OUTER JOIN [dbo].[CustomerContent] AS [cc] ON c.Id = cc.CustomerId
  LEFT OUTER JOIN [dbo].[Forums_PrivateMessage] AS [pm] ON c.Id = pm.ToCustomerId
  LEFT OUTER JOIN [dbo].[Forums_Post] AS [fp] ON c.Id = fp.CustomerId
  LEFT OUTER JOIN [dbo].[Forums_Topic] AS [ft] ON c.Id = ft.CustomerId
  WHERE g.KeyGroup = 'Customer' AND c.Username IS Null AND c.Email IS NULL AND c.IsSystemAccount = 0{0}
	AND (NOT EXISTS (SELECT 1 AS [C1] FROM [dbo].[Order] AS [o1] WHERE c.Id = o1.CustomerId ))
	AND (NOT EXISTS (SELECT 1 AS [C1] FROM [dbo].[CustomerContent] AS [cc1] WHERE c.Id = cc1.CustomerId ))
	AND (NOT EXISTS (SELECT 1 AS [C1] FROM [dbo].[Forums_PrivateMessage] AS [pm1] WHERE c.Id = pm1.ToCustomerId ))
	AND (NOT EXISTS (SELECT 1 AS [C1] FROM [dbo].[Forums_Post] AS [fp1] WHERE c.Id = fp1.CustomerId ))
	AND (NOT EXISTS (SELECT 1 AS [C1] FROM [dbo].[Forums_Topic] AS [ft1] WHERE c.Id = ft1.CustomerId ))
";

		const string SqlGuestCustomers = @"
DELETE TOP(20000) [c]
  FROM [dbo].[Customer] AS [c]
  LEFT OUTER JOIN [dbo].[Order] AS [o] ON c.Id = o.CustomerId
  LEFT OUTER JOIN [dbo].[CustomerContent] AS [cc] ON c.Id = cc.CustomerId
  LEFT OUTER JOIN [dbo].[Forums_PrivateMessage] AS [pm] ON c.Id = pm.ToCustomerId
  LEFT OUTER JOIN [dbo].[Forums_Post] AS [fp] ON c.Id = fp.CustomerId
  LEFT OUTER JOIN [dbo].[Forums_Topic] AS [ft] ON c.Id = ft.CustomerId
  WHERE c.Username IS Null AND c.Email IS NULL AND c.IsSystemAccount = 0{0}
	AND (NOT EXISTS (SELECT 1 AS x FROM [dbo].[Order] AS [o1] WHERE c.Id = o1.CustomerId ))
	AND (NOT EXISTS (SELECT 1 AS x FROM [dbo].[CustomerContent] AS [cc1] WHERE c.Id = cc1.CustomerId ))
	AND (NOT EXISTS (SELECT 1 AS x FROM [dbo].[Forums_PrivateMessage] AS [pm1] WHERE c.Id = pm1.ToCustomerId ))
	AND (NOT EXISTS (SELECT 1 AS x FROM [dbo].[Forums_Post] AS [fp1] WHERE c.Id = fp1.CustomerId ))
	AND (NOT EXISTS (SELECT 1 AS x FROM [dbo].[Forums_Topic] AS [ft1] WHERE c.Id = ft1.CustomerId ))
";
		#endregion

		private readonly SmartDbContext _db;
		private readonly UserManager<Customer> _userManager;
		private readonly IWebHelper _webHelper;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IUserAgent _userAgent;
		private readonly IChronometer _chronometer;

		private Customer _authCustomer;
		private bool _authCustomerResolved;

		public CustomerService(
			SmartDbContext db,
			UserManager<Customer> userManager,
			IWebHelper webHelper,
			IHttpContextAccessor httpContextAccessor,
			IUserAgent userAgent,
			IChronometer chronometer)
        {
            _db = db;
			_userManager = userManager;
			_webHelper = webHelper;
			_httpContextAccessor = httpContextAccessor;
			_userAgent = userAgent;
			_chronometer = chronometer;
        }

		public ILogger Logger { get; set; } = NullLogger.Instance;

        #region Hook

        protected override Task<HookResult> OnUpdatingAsync(Customer entity, IHookedEntity entry, CancellationToken cancelToken)
			=> Task.FromResult(HookResult.Ok);

		protected override Task<HookResult> OnInsertingAsync(Customer entity, IHookedEntity entry, CancellationToken cancelToken)
			=> Task.FromResult(HookResult.Ok);

		public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
		{
			var softDeletedCustomer = entries
				.Where(x => x.IsSoftDeleted == true)
				.Select(x => x.Entity)
				.OfType<Customer>()
				.FirstOrDefault();

			if (softDeletedCustomer?.IsSystemAccount ?? false)
			{
				throw new NotSupportedException($"System customer account ({softDeletedCustomer.SystemName}) cannot not be deleted.");
			}

			// TODO: (mg) (core) Validate unique user when inserting customer.

			return Task.FromResult(HookResult.Ok);
		}

		#endregion

		#region Guest customers

		public virtual async Task<Customer> CreateGuestCustomerAsync(Guid? customerGuid = null)
		{
			var customer = new Customer
			{
				CustomerGuid = customerGuid ?? Guid.NewGuid(),
				Active = true,
				CreatedOnUtc = DateTime.UtcNow,
				LastActivityDateUtc = DateTime.UtcNow,
			};

			// Add to 'Guests' role
			var guestRole = await GetRoleBySystemNameAsync(SystemCustomerRoleNames.Guests);
			if (guestRole == null)
			{
				throw new SmartException("'Guests' role could not be loaded");
			}

			// Ensure that entities are saved to db in any case
			customer.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = guestRole.Id });
			_db.Customers.Add(customer);

			await _db.SaveChangesAsync();

			var clientIdent = _webHelper.GetClientIdent();
			if (clientIdent.HasValue())
			{
				customer.GenericAttributes.ClientIdent = clientIdent;
				await _db.SaveChangesAsync();
			}

			//Logger.DebugFormat("Guest account created for anonymous visitor. Id: {0}, ClientIdent: {1}", customer.CustomerGuid, clientIdent ?? "n/a");

			return customer;
		}

		public virtual Task<Customer> FindGuestCustomerByClientIdentAsync(string clientIdent = null, int maxAgeSeconds = 60)
		{
			if (_httpContextAccessor.HttpContext == null || _userAgent.IsBot || _userAgent.IsPdfConverter)
			{
				return Task.FromResult<Customer>(null);
			}

			using (_chronometer.Step("FindGuestCustomerByClientIdent"))
			{
				clientIdent = clientIdent.NullEmpty() ?? _webHelper.GetClientIdent();
				if (clientIdent.IsEmpty())
				{
					return Task.FromResult<Customer>(null);
				}

				var dateFrom = DateTime.UtcNow.AddSeconds(-maxAgeSeconds);

				var query = from a in _db.GenericAttributes.AsNoTracking()
						join c in _db.Customers on a.EntityId equals c.Id into Customers
						from c in Customers.DefaultIfEmpty()
						where c.LastActivityDateUtc >= dateFrom
							&& c.Username == null
							&& c.Email == null
							&& a.KeyGroup == "Customer"
							&& a.Key == "ClientIdent"
							&& a.Value == clientIdent
						select c;

				return query
					.IncludeCustomerRoles()
					.IncludeShoppingCart()
					.FirstOrDefaultAsync();
			}
		}

		public virtual async Task<int> DeleteGuestCustomersAsync(
			DateTime? registrationFrom,
			DateTime? registrationTo,
			bool onlyWithoutShoppingCart)
		{
			var paramClauses = new StringBuilder();
			var parameters = new List<object>();
			var numberOfDeletedCustomers = 0;
			var numberOfDeletedAttributes = 0;
			var pIndex = 0;

			if (registrationFrom.HasValue)
			{
				paramClauses.AppendFormat(" AND @p{0} <= c.CreatedOnUtc", pIndex++);
				parameters.Add(registrationFrom.Value);
			}
			if (registrationTo.HasValue)
			{
				paramClauses.AppendFormat(" AND @p{0} >= c.CreatedOnUtc", pIndex++);
				parameters.Add(registrationTo.Value);
			}
			if (onlyWithoutShoppingCart)
			{
				paramClauses.Append(" AND (NOT EXISTS (SELECT 1 AS [C1] FROM [dbo].[ShoppingCartItem] AS [sci] WHERE c.Id = sci.CustomerId ))");
			}

			var sqlGenericAttributes = SqlGenericAttributes.FormatInvariant(paramClauses.ToString());
			var sqlGuestCustomers = SqlGuestCustomers.FormatInvariant(paramClauses.ToString());

			// Delete generic attributes.
			while (true)
			{
				var numDeleted = await _db.Database.ExecuteSqlRawAsync(sqlGenericAttributes, parameters.ToArray());
				if (numDeleted <= 0)
				{
					break;
				}

				numberOfDeletedAttributes += numDeleted;
			}

			// Delete guest customers.
			while (true)
			{
				var numDeleted = await _db.Database.ExecuteSqlRawAsync(sqlGuestCustomers, parameters.ToArray());
				if (numDeleted <= 0)
				{
					break;
				}

				numberOfDeletedCustomers += numDeleted;
			}

			Logger.Debug("Deleted {0} guest customers including {1} generic attributes.", numberOfDeletedCustomers, numberOfDeletedAttributes);

			return numberOfDeletedCustomers;
		}

		#endregion

		#region Customers

		public virtual Customer GetCustomerBySystemName(string systemName, bool tracked = true)
		{
			if (string.IsNullOrWhiteSpace(systemName))
				return null;

			var query = _db.Customers
				.IncludeCustomerRoles()
				.ApplyTracking(tracked)
				.AsCaching()
				.Where(x => x.SystemName == systemName)
				.OrderBy(x => x.Id);

			return query.FirstOrDefault();
		}

		public virtual Task<Customer> GetCustomerBySystemNameAsync(string systemName, bool tracked = true)
		{
			if (string.IsNullOrWhiteSpace(systemName))
				return Task.FromResult((Customer)null);

			var query = _db.Customers
				.IncludeCustomerRoles()
				.ApplyTracking(tracked)
				.AsCaching()
				.Where(x => x.SystemName == systemName)
				.OrderBy(x => x.Id);

			return query.FirstOrDefaultAsync();
		}

		public virtual async Task<Customer> GetAuthenticatedCustomerAsync()
        {
			if (!_authCustomerResolved)
            {
				var httpContext = _httpContextAccessor.HttpContext;
				if (httpContext == null)
				{
					return null;
				}

				var principal = await EnsureAuthentication(httpContext);

				if (principal?.Identity.IsAuthenticated == true)
				{
					_authCustomer = await _userManager.GetUserAsync(principal);
				}

				_authCustomerResolved = true;
			}

			if (_authCustomer == null || !_authCustomer.Active || _authCustomer.Deleted || !_authCustomer.IsRegistered())
			{
				return null;
			}

			return _authCustomer;
		}

		/// <summary>
		/// Ensures that the authentication handler runs (even before the authentication middleware)
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		private static async Task<ClaimsPrincipal> EnsureAuthentication(HttpContext context)
        {
			var authenticationFeature = context.Features.Get<IAuthenticationFeature>();
			if (authenticationFeature == null)
            {
				// The middleware did not run yet
				var result = await context.AuthenticateAsync();
				if (result.Succeeded)
                {
					return result.Principal;
                }
            }

			return context.User;
		}

		#endregion

		#region Roles

		public virtual CustomerRole GetRoleBySystemName(string systemName, bool tracked = true)
		{
			if (string.IsNullOrWhiteSpace(systemName))
				return null;

			var query = _db.CustomerRoles
				.ApplyTracking(tracked)
				.AsCaching()
				.Where(x => x.SystemName == systemName)
				.OrderBy(x => x.Id);

			return query.FirstOrDefault();
		}

		public virtual Task<CustomerRole> GetRoleBySystemNameAsync(string systemName, bool tracked = true)
		{
			if (string.IsNullOrWhiteSpace(systemName))
				return Task.FromResult((CustomerRole)null);

			var query = _db.CustomerRoles
				.ApplyTracking(tracked)
				.AsCaching()
				.Where(x => x.SystemName == systemName)
				.OrderBy(x => x.Id);

			return query.FirstOrDefaultAsync();
		}

		#endregion
	}
}
