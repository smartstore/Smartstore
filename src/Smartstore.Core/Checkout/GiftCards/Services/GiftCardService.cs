using System.Linq.Dynamic.Core;
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
            var result = new List<AppliedGiftCard>();
            var couponCodes = customer?.GenericAttributes?.GiftCardCouponCodes;

            if (couponCodes?.Any() ?? false)
            {
                var giftCards = await _db.GiftCards
                    .Include(x => x.PurchasedWithOrderItem).ThenInclude(x => x.Order)
                    .Include(x => x.GiftCardUsageHistory)
                    .AsSplitQuery()
                    .ApplyCouponFilter(couponCodes.Select(x => x.Value).ToArray())
                    .ApplyStandardFilter()
                    .ToListAsync();

                foreach (var giftCard in giftCards)
                {
                    var remainingAmount = await GetRemainingAmountCoreAsync(giftCard, true, storeId);
                    if (remainingAmount > decimal.Zero)
                    {
                        result.Add(new AppliedGiftCard
                        {
                            GiftCard = giftCard,
                            UsableAmount = new(remainingAmount, _primaryCurrency)
                        });
                    }
                }
            }

            return result;
        }

        public virtual async Task<bool> ValidateGiftCardAsync(GiftCard giftCard, int storeId = 0)
        {
            Guard.NotNull(giftCard, nameof(giftCard));

            return await GetRemainingAmountCoreAsync(giftCard, true, storeId) > decimal.Zero;
        }

        public virtual async Task<Money> GetRemainingAmountAsync(GiftCard giftCard)
        {
            Guard.NotNull(giftCard, nameof(giftCard));

            var amount = await GetRemainingAmountCoreAsync(giftCard, false);

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

        protected virtual async Task<decimal> GetRemainingAmountCoreAsync(GiftCard giftCard, bool validate, int storeId = 0)
        {
            if (validate)
            {
                await _db.LoadReferenceAsync(giftCard, x => x.PurchasedWithOrderItem, false, q => q.Include(x => x.Order));

                if (!giftCard.IsGiftCardActivated)
                {
                    return decimal.Zero;
                }

                if (storeId != 0
                    && giftCard.PurchasedWithOrderItemId.HasValue
                    && giftCard.PurchasedWithOrderItem?.Order != null
                    && giftCard.PurchasedWithOrderItem.Order.StoreId != storeId)
                {
                    return decimal.Zero;
                }
            }

            await _db.LoadCollectionAsync(giftCard, x => x.GiftCardUsageHistory);

            var usedValue = giftCard.GiftCardUsageHistory.Sum(x => x.UsedValue);

            return giftCard.Amount - usedValue;
        }
    }
}