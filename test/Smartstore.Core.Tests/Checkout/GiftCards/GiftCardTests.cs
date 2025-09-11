using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Checkout.GiftCards
{
    [TestFixture]
    public class GiftCardTests : ServiceTestBase
    {
        IGiftCardService _giftCardService;
        ICurrencyService _currencyService;

        [SetUp]
        public new void SetUp()
        {
            //base.SetUp();

            _currencyService = new CurrencyService(
                DbContext,
                null,
                null,
                null,
                new() { PrimaryCurrencyId = 1 },
                null, 
                null)
            {
                PrimaryCurrency = new()
            };

            _giftCardService = new GiftCardService(DbContext, _currencyService);
        }

        [Test]
        public async Task Can_validate_gift_card()
        {
            var customer = new Customer
            {
                Id = 1
            };

            var gc = new GiftCard
            {
                Amount = 100,
                IsGiftCardActivated = true,
                PurchasedWithOrderItemId = 2,
                PurchasedWithOrderItem = new()
                {
                    Id = 2,
                    OrderId = 1,
                    Order = new Order
                    {
                        StoreId = 1
                    }
                }
            };

            gc.GiftCardUsageHistory.Add(new() { UsedValue = 30 });
            gc.GiftCardUsageHistory.Add(new() { UsedValue = 20 });
            gc.GiftCardUsageHistory.Add(new() { UsedValue = 5 });

            // valid
            (await _giftCardService.ValidateGiftCardAsync(gc, customer, 1)).ShouldEqual(true);

            // wrong store
            (await _giftCardService.ValidateGiftCardAsync(gc, customer, 2)).ShouldEqual(false);

            // mark as not active
            gc.IsGiftCardActivated = false;
            (await _giftCardService.ValidateGiftCardAsync(gc, customer, 1)).ShouldEqual(false);

            // again active
            gc.IsGiftCardActivated = true;
            (await _giftCardService.ValidateGiftCardAsync(gc, customer, 1)).ShouldEqual(true);

            // add usage history record
            gc.GiftCardUsageHistory.Add(new GiftCardUsageHistory { UsedValue = 1000 });
            (await _giftCardService.ValidateGiftCardAsync(gc, customer, 1)).ShouldEqual(false);
        }

        [Test]
        public async Task Can_calculate_gift_card_remaining_amount()
        {
            var gc = new GiftCard
            {
                Amount = 100
            };

            gc.GiftCardUsageHistory.Add(new() { UsedValue = 30 });
            gc.GiftCardUsageHistory.Add(new() { UsedValue = 20 });
            gc.GiftCardUsageHistory.Add(new() { UsedValue = 5 });

            (await _giftCardService.GetRemainingAmountAsync(gc)).Amount.ShouldEqual(45);
        }
    }
}