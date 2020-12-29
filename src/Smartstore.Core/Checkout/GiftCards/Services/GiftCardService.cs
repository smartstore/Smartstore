using System;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.GiftCards.Extensions;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.GiftCards
{
    public partial class GiftCardService : IGiftCardService
    {
        private readonly SmartDbContext _db;

        public GiftCardService(SmartDbContext db)
        {
            _db = db;
        }

        //TODO: (core) (ms) customer extension ParseAppliedGiftCardCouponCodes is needed + nav props (order item) > load eager(include)
        //public virtual async Task<List<GiftCard>> GetActiveGiftCardsAppliedByCustomerAsync(Customer customer, int storeId)
        //{
        //    var result = new List<GiftCard>();
        //    if (customer == null)
        //        return result;

        //    // Get existing gift card codes by customer
        //    string[] couponCodes = await customer.ParseAppliedGiftCardCouponCodes();

        //    var giftCards = await _db.GiftCards
        //        .Include(x => x.OrderItem)
        //        .ThenInclude(x => x.Order)                
        //        .Where(x => x.IsActivated)
        //        .Where(x => couponCodes.Contains(x.CouponCode))
        //        .ToListAsync();

        //    foreach (var giftCard in giftCards)
        //    {
        //        if (giftCard.IsValid(storeId))
        //        {
        //            result.Add(giftCard);
        //        }
        //    }

        //    return result;
        //}

        public virtual Task<string> GenerateGiftCardCodeAsync()
        {
            var length = 13;
            var result = Guid.NewGuid().ToString();
            if (result.Length > length)
            {
                result = result.Substring(0, length);
            }

            return Task.FromResult(result);
        }
    }
}
