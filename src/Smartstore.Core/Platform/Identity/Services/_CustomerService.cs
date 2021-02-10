using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Collections;
using Smartstore.Core.Data;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Identity
{
    public partial class CustomerService : ICustomerService
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
		private readonly CustomerSettings _customerSettings;

		public CustomerService(
			SmartDbContext db,
			CustomerSettings customerSettings)
        {
            _db = db;
			_customerSettings = customerSettings;
        }

		public ILogger Logger { get; set; } = NullLogger.Instance;

		#region Customers

		public virtual Task<Customer> GetCustomerBySystemNameAsync(string systemName, bool tracked = true)
		{
			if (string.IsNullOrWhiteSpace(systemName))
				return Task.FromResult((Customer)null);

			var query = _db.Customers
				.ApplyTracking(tracked)
				.AsCaching()
				.Where(x => x.SystemName == systemName)
				.OrderBy(x => x.Id);

			return query.FirstOrDefaultAsync();
		}

		public IQueryable<Customer> BuildSearchQuery(CustomerSearchQuery q)
        {
            Guard.NotNull(q, nameof(q));

            var isOrdered = false;
            IQueryable<Customer> query = null;

			if (q.OnlyWithCart)
			{
				var cartItemQuery = _db.ShoppingCartItems
					.AsNoTracking()
					.Include(x => x.Customer)
					.AsQueryable();

				if (q.CartType.HasValue)
				{
					cartItemQuery = cartItemQuery.Where(x => x.ShoppingCartTypeId == (int)q.CartType.Value);
				}

				var groupQuery =
					from sci in cartItemQuery
					group sci by sci.CustomerId into grp
					select grp
						.OrderByDescending(x => x.CreatedOnUtc)
						.Select(x => new
						{
							x.Customer,
							x.CreatedOnUtc
						})
						.FirstOrDefault();

				// We have to sort again because of paging.
				query = groupQuery
					.OrderByDescending(x => x.CreatedOnUtc)
					.Select(x => x.Customer);

				isOrdered = true;
			}
			else
			{
				query = _db.Customers;
			}

			if (q.Email.HasValue())
			{
				query = query.Where(c => c.Email.Contains(q.Email));
			}

			if (q.Username.HasValue())
			{
				query = query.Where(c => c.Username.Contains(q.Username));
			}

			if (q.CustomerNumber.HasValue())
			{
				query = query.Where(c => c.CustomerNumber.Contains(q.CustomerNumber));
			}

			if (q.AffiliateId.GetValueOrDefault() > 0)
			{
				query = query.Where(c => c.AffiliateId == q.AffiliateId.Value);
			}

			if (q.SearchTerm.HasValue())
			{
				if (_customerSettings.CompanyEnabled)
				{
					query = query.Where(c => c.FullName.Contains(q.SearchTerm) || c.Company.Contains(q.SearchTerm));
				}
				else
				{
					query = query.Where(c => c.FullName.Contains(q.SearchTerm));
				}
			}

			if (q.DayOfBirth > 0)
			{
				query = query.Where(c => c.BirthDate.Value.Day == q.DayOfBirth.Value);
			}

			if (q.MonthOfBirth > 0)
			{
				query = query.Where(c => c.BirthDate.Value.Month == q.MonthOfBirth.Value);
			}

			if (q.RegistrationFromUtc.HasValue)
			{
				query = query.Where(c => q.RegistrationFromUtc.Value <= c.CreatedOnUtc);
			}

			if (q.RegistrationToUtc.HasValue)
			{
				query = query.Where(c => q.RegistrationToUtc.Value >= c.CreatedOnUtc);
			}

			if (q.LastActivityFromUtc.HasValue)
			{
				query = query.Where(c => q.LastActivityFromUtc.Value <= c.LastActivityDateUtc);
			}

			if (q.CustomerRoleIds != null && q.CustomerRoleIds.Length > 0)
			{
				query = query.Where(c => c.CustomerRoleMappings.Select(rm => rm.CustomerRoleId).Intersect(q.CustomerRoleIds).Any());
			}

			if (q.Deleted.HasValue)
			{
				query = query.Where(c => c.Deleted == q.Deleted.Value);
			}

			if (q.Active.HasValue)
			{
				query = query.Where(c => c.Active == q.Active.Value);
			}

			if (q.IsSystemAccount.HasValue)
			{
				//query = q.IsSystemAccount.Value == true
				//	? query.Where(c => !string.IsNullOrEmpty(c.SystemName))
				//	: query.Where(c => string.IsNullOrEmpty(c.SystemName));
				query = query.Where(c => c.IsSystemAccount == q.IsSystemAccount.Value);
			}

			if (q.PasswordFormat.HasValue)
			{
				int passwordFormatId = (int)q.PasswordFormat.Value;
				query = query.Where(c => c.PasswordFormatId == passwordFormatId);
			}

			// Search by phone
			if (q.Phone.HasValue())
			{
				query = query
					.Join(_db.GenericAttributes, x => x.Id, y => y.EntityId, (x, y) => new { Customer = x, Attribute = y })
					.Where(z => z.Attribute.KeyGroup == nameof(Customer) &&
						z.Attribute.Key == SystemCustomerAttributeNames.Phone &&
						z.Attribute.Value.Contains(q.Phone))
					.Select(z => z.Customer);
			}

			// Search by zip
			if (q.ZipPostalCode.HasValue())
			{
				query = query
					.Join(_db.GenericAttributes, x => x.Id, y => y.EntityId, (x, y) => new { Customer = x, Attribute = y })
					.Where(z => z.Attribute.KeyGroup == nameof(Customer) &&
						z.Attribute.Key == SystemCustomerAttributeNames.ZipPostalCode &&
						z.Attribute.Value.Contains(q.ZipPostalCode))
					.Select(z => z.Customer);
			}

			if (!isOrdered)
			{
				query = query.OrderByDescending(c => c.CreatedOnUtc);
			}

			return query.ApplyPaging(q.PageIndex, q.PageSize);
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

		#region Roles

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
