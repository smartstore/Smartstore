using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Identity
{
    public static partial class CustomerQueryExtensions
    {
        /// <summary>
        /// Selects only customers with shopping carts and sorts by <see cref="Customer.CreatedOnUtc"/> descending.
        /// </summary>
        /// <param name="cartType">Type of cart to match. <c>null</c> to match any type.</param>
        public static IQueryable<Customer> ApplyHasCartFilter(this IQueryable<Customer> query, ShoppingCartType? cartType = null)
        {
            Guard.NotNull(query, nameof(query));

            var cartItemQuery = query
                .GetDbContext<SmartDbContext>()
                .ShoppingCartItems
                .AsNoTracking()
                .Include(x => x.Customer)
                .AsQueryable();

            if (cartType.HasValue)
            {
                cartItemQuery = cartItemQuery.Where(x => x.ShoppingCartTypeId == (int)cartType.Value);
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

            return query;
        }
    }
}
