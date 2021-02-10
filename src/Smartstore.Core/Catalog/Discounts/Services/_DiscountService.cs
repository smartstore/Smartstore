using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Catalog.Discounts
{
    public partial class DiscountService : IDiscountService
    {
        private readonly SmartDbContext _db;
        private readonly IRequestCache _requestCache;

        public DiscountService(SmartDbContext db, IRequestCache requestCache)
        {
            _db = db;
            _requestCache = requestCache;
        }

        protected virtual async Task<bool> CheckDiscountLimitationsAsync(Discount discount, Customer customer)
        {
            Guard.NotNull(discount, nameof(discount));

            switch (discount.DiscountLimitation)
            {
                case DiscountLimitationType.NTimesOnly:
                    {
                        var count = await _db.DiscountUsageHistory
                            .ApplyStandardFilter(discount.Id)
                            .CountAsync();
                        return count < discount.LimitationTimes;
                    }

                case DiscountLimitationType.NTimesPerCustomer:
                    if (customer != null && !customer.IsGuest())
                    {
                        // Registered customer.
                        var count = await _db.DiscountUsageHistory
                            .Include(x => x.Order)
                            .ApplyStandardFilter(discount.Id, customer.Id)
                            .CountAsync();
                        return count < discount.LimitationTimes;
                    }
                    else
                    {
                        // Guest.
                        return true;
                    }

                case DiscountLimitationType.Unlimited:
                    return true;

                default:
                    return false;
            }
        }
    }
}
