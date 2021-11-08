using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.GiftCards
{
    public partial class GiftCardService : IGiftCardService
    {
        private readonly SmartDbContext _db;
        private readonly Currency _primaryCurrency;

        public GiftCardService(SmartDbContext db, ICurrencyService currencyService)
        {
            _db = db;
            _primaryCurrency = currencyService.PrimaryCurrency;
        }

        public virtual async Task<List<AppliedGiftCard>> GetValidGiftCardsAsync(int storeId = 0, Customer customer = null)
        {
            var query = _db.GiftCards
                .Include(x => x.PurchasedWithOrderItem)
                    .ThenInclude(x => x.Order)
                .Include(x => x.GiftCardUsageHistory)
                .ApplyStandardFilter(storeId)
                .AsQueryable();

            if (customer != null)
            {
                // Get gift card codes applied by customer
                var couponCodes = customer.GenericAttributes.GiftCardCouponCodes;
                if (!couponCodes.Any())
                {
                    return new List<AppliedGiftCard>();
                }

                query = query.ApplyCouponFilter(couponCodes.Select(x => x.Value).ToArray());
            }

            // Get valid gift cards (remaining useable amount > 0)
            var giftCards = await query
                .Select(x => new
                {
                    GiftCard = x,
                    UsableAmount = x.Amount - x.GiftCardUsageHistory.Where(y => y.GiftCardId == x.Id).Sum(x => x.UsedValue)
                })
                .Where(x => x.UsableAmount > decimal.Zero)
                .ToListAsync();

            return giftCards.Select(x => new AppliedGiftCard
            {
                GiftCard = x.GiftCard,
                UsableAmount = new(x.UsableAmount, _primaryCurrency)
            })
            .ToList();
        }

        public virtual async Task<bool> ValidateGiftCardAsync(GiftCard giftCard, int storeId = 0)
        {
            Guard.NotNull(giftCard, nameof(giftCard));

            if (!giftCard.IsGiftCardActivated)
            {
                return false;
            }

            if (storeId != 0 
                && giftCard.PurchasedWithOrderItemId.HasValue 
                && giftCard.PurchasedWithOrderItem?.Order != null 
                && giftCard.PurchasedWithOrderItem.Order.StoreId != storeId)
            {
                return false;
            }

            return await GetRemainingAmountCoreAsync(giftCard) > decimal.Zero;

            //var orderStoreId = giftCard.PurchasedWithOrderItem?.Order?.StoreId ?? null;
            //return (storeId == 0 || orderStoreId is null || orderStoreId == storeId) && GetRemainingAmount(giftCard) > decimal.Zero;
        }

        public virtual async Task<Money> GetRemainingAmountAsync(GiftCard giftCard)
        {
            Guard.NotNull(giftCard, nameof(giftCard));

            var amount = await GetRemainingAmountCoreAsync(giftCard);

            return new(Math.Max(amount, decimal.Zero), _primaryCurrency);
        }

        public virtual string GenerateGiftCardCode()
        {
            const int length = 13;
            var result = Guid.NewGuid().ToString();

            if (result.Length > length)
            {
                result = result.Substring(0, length);
            }

            return result;
        }

        protected virtual async Task<decimal> GetRemainingAmountCoreAsync(GiftCard giftCard)
        {
            await _db.LoadCollectionAsync(giftCard, x => x.GiftCardUsageHistory);

            var usedValue = giftCard.GiftCardUsageHistory.Sum(x => x.UsedValue);

            return giftCard.Amount - usedValue;
        }
    }
}