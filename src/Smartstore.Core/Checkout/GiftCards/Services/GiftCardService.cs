using Smartstore.Core.Data;
using System;
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

        //TODO: (core) (ms) Customer.ParseAppliedGiftCardCouponCodes will be removed -> nav props: load eager (include)
        //public virtual async Task<IList<GiftCard>> GetActiveGiftCardsAppliedByCustomerAsync(Customer customer, int storeId)
        //{
        //    var result = new List<GiftCard>();
        //    if (customer == null)
        //        return result;

        //    string[] couponCodes = await customer.ParseAppliedGiftCardCouponCodes();

        //    var giftCards = await _db.GiftCards
        //        .ApplyCouponCodeFilter(couponCodes)
        //        .Where(x => x.IsActivated)
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
