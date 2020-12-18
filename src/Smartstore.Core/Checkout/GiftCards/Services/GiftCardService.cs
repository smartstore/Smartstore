using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Customers;
using Smartstore.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Core.Checkout.GiftCards
{
    public partial class GiftCardService
    {
        private readonly SmartDbContext _db;

        public GiftCardService(SmartDbContext db)
        {
            _db = db;
        }

        // TODO: (core) (ms) Customer.ParseAppliedGiftCardCouponCodes extension is needed
        /// <summary>
        /// Gets active gift cards that are applied by customer
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>List of active gift cards</returns>
        //public async virtual Task<IList<GiftCard>> GetActiveGiftCardsAppliedByCustomerAsync(Customer customer, int storeId)
        //{
        //    var result = new List<GiftCard>();
        //    if (customer == null)
        //        return result;

        //    string[] couponCodes = await customer.ParseAppliedGiftCardCouponCodes();

        //    var giftCards = await GetAllGiftCardsQuery()
        //        .ApplyCouponCodeFilter(couponCodes)
        //        .ApplyActivationFilter(true)
        //        .ToListAsync();

        //    foreach (var giftCard in giftCards)
        //    {
        //        if (giftCard.IsValidGiftCard(storeId))
        //            result.Add(giftCard);
        //    }

        //    return result;
        //}

        public virtual Task<string> GenerateGiftCardCodeAsync()
        {
            var length = 13;
            var result = Guid.NewGuid().ToString();
            if (result.Length > length)
                result = result.Substring(0, length);

            return Task.FromResult(result);
        }
    }
}
