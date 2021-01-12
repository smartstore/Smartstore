using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Customers;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.GiftCards
{
    public partial class GiftCardService : IGiftCardService
    {
        private readonly SmartDbContext _db;
        private readonly IGenericAttributeService _attributeService;

        public GiftCardService(SmartDbContext db)
        {
            _db = db;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        //TODO: (ms) (core) customer extension ParseAppliedGiftCardCouponCodes is needed + nav props (order item) > load eager(include)
        //public virtual async Task<List<GiftCard>> GetActiveGiftCardsAppliedByCustomerAsync(Customer customer, int storeId)
        //{
        //    var result = new List<GiftCard>();
        //    if (customer == null)
        //        return result;
        public virtual async Task<List<AppliedGiftCard>> GetAppliedGiftCardsByCustomerAsync(Customer customer, int storeId = 0)
        {
            Guard.NotNull(customer, nameof(customer));

            var couponCodeXml = customer.GenericAttributes.GiftCardCouponCodes;

            if (couponCodeXml.IsEmpty())
                return new();

            var query = _db.GiftCards
                .Include(x => x.PurchasedWithOrderItem)
                .ThenInclude(x => x.Order)
                .Where(x => x.IsGiftCardActivated);

            // Get existing gift card codes by customer            
            try
            {
                var xel = XElement.Parse(couponCodeXml);
                var couponCodes = xel.Descendants("CouponCode").Select(x => x.Attribute("Code").Value).ToList();
                query = query.Where(x => couponCodes.Contains(x.GiftCardCouponCode));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occured while parsing gift card from XML");
                return new();
            }

            // Get gift cards usable amounts left
            var usableAmounts = await _db.GiftCardUsageHistory
                .Where(x => query.Select(x => x.Id).Contains(x.GiftCardId))
                .GroupBy(x => x.GiftCardId)
                .Select(x => new { UsableAmount = x.Sum(y => y.UsedValue), Id = x.Key })
                .Where(x => x.UsableAmount > decimal.Zero)
                .Select(x => x)
                .ToListAsync();

            var appliedGiftCards = await query.Where(x => (storeId == 0 || x.PurchasedWithOrderItem.Order.StoreId == 0 || x.PurchasedWithOrderItem.Order.StoreId == storeId) 
                && usableAmounts.Select(y => y.Id).Contains(x.Id))
                .Select(x => new AppliedGiftCard
                {
                    UsableAmount = usableAmounts.Where(y => y.Id == x.Id).Select(y => y.UsableAmount).FirstOrDefault(),
                    GiftCard = x
                })
                .ToListAsync();

            return appliedGiftCards;
        }

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

        //public virtual async IQueryable<GiftCard> GetValidGiftCardsAsync(int storeId = 0)
        //{
        //    var query = _db.GiftCards
        //        .Where(x => x.IsGiftCardActivated && x.Amount > decimal.Zero)
        //        .Where(x => x.GiftCardCouponCode)

        //    // Get gift card identifiers with valid remaining value
        //    var validQuery = _db.GiftCardUsageHistory
        //        .Where(x => query.Select(x => x.Id).Contains(x.GiftCardId))
        //        .GroupBy(x => x.GiftCardId)
        //        .Where(x => x.Sum(y => y.UsedValue) > decimal.Zero)
        //        .Select(x => x.Key);

        //    query.EachAsync(async x =>
        //    {
        //        if (!_db.IsReferenceLoaded(x, y => y.PurchasedWithOrderItem))
        //        {
        //            _db.LoadReferenceAsync(x, y => y.PurchasedWithOrderItem).Await();
        //        }
        //        if (_db.IsReferenceLoaded(x, y => y.PurchasedWithOrderItem.Order))
        //        {
        //            await _db.LoadReferenceAsync(x, y => y.PurchasedWithOrderItem.Order);
        //        }
        //    }).Await();

        //    return query.Where(x =>
        //           (storeId == 0 || x.PurchasedWithOrderItem.Order.StoreId == 0 || x.PurchasedWithOrderItem.Order.StoreId == storeId)
        //           && validQuery.Contains(x.Id));
        //}

        //TODO: (ms) (core) have order item & order eager loaded
        public virtual bool ValidateGiftCard(GiftCard giftCard, int storeId = 0)
        {
            Guard.NotNull(giftCard, nameof(giftCard));

            if (!giftCard.IsGiftCardActivated)
                return false;

            // TODO: (ms) (core) Check whether dbContext has OrderItem + Order already loaded => just access store id
            // TODO: (ms) (core) Write a query that fetches required info in one roundtrip!

            var orderStoreId = giftCard.PurchasedWithOrderItem?.Order?.StoreId ?? null;
            return (storeId == 0 || orderStoreId is null || orderStoreId == storeId) && GetRemainingAmount(giftCard) > decimal.Zero;
        }

        //TODO: (ms) (core) have giftcard usage history eager loaded
        public virtual decimal GetRemainingAmount(GiftCard giftCard)
        {
            Guard.NotNull(giftCard, nameof(giftCard));

            var result = giftCard.Amount - giftCard.GiftCardUsageHistory.Sum(x => x.UsedValue);
            return result < decimal.Zero
                ? decimal.Zero
                : result;
        }
    }
}