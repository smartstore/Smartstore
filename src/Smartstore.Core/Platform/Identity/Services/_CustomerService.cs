using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core.Data;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Identity
{
    public partial class CustomerService : ICustomerService
    {
        // TODO: (core) finish CustomerService.

        private readonly SmartDbContext _db;
		private readonly CustomerSettings _customerSettings;

		public CustomerService(
			SmartDbContext db,
			CustomerSettings customerSettings)
        {
            _db = db;
			_customerSettings = customerSettings;
        }

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

		public IQueryable<Customer> BuildQuery(CustomerSearchQuery q)
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

        #endregion
    }
}
