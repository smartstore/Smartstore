using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Identity
{
    public partial class CustomerService : ICustomerService
    {
        // TODO: (core) finish CustomerService.

        private readonly SmartDbContext _db;

        public CustomerService(SmartDbContext db)
        {
            _db = db;
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

        #endregion
    }
}
