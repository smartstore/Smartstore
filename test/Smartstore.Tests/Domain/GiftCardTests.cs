using System.Threading.Tasks;
using NUnit.Framework;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Test.Common;

namespace Smartstore.Tests.Domain
{
    [TestFixture]
    public class GiftCardTests : TestsBase
    {
        IGiftCardService _giftCardService;
        ICurrencyService _currencyService;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _currencyService = new CurrencyService(
                null,
                null,
                null,
                null,
                new CurrencySettings { PrimaryCurrencyId = 1 },
                new TaxSettings { DefaultTaxAddressId = 10, EuVatUseWebService = true },
                null)
            {
                PrimaryCurrency = new Currency()
            };

            _giftCardService = new GiftCardService(null, _currencyService);
        }

        [Test]
        public async Task Can_validate_giftCardAsync()
        {
            var gc = new GiftCard
            {
                Amount = 100,
                IsGiftCardActivated = true,
                PurchasedWithOrderItemId = 2,
                PurchasedWithOrderItem = new OrderItem
                {
                    Id = 2,
                    OrderId = 1,
                    Order = new Order
                    {
                        StoreId = 1
                    }
                }
            };

            gc.GiftCardUsageHistory.Add(new GiftCardUsageHistory() { UsedValue = 30 });
            gc.GiftCardUsageHistory.Add(new GiftCardUsageHistory() { UsedValue = 20 });
            gc.GiftCardUsageHistory.Add(new GiftCardUsageHistory() { UsedValue = 5 });

            // valid
            (await _giftCardService.ValidateGiftCardAsync(gc, 1)).ShouldEqual(true);

            // wrong store
            (await _giftCardService.ValidateGiftCardAsync(gc, 2)).ShouldEqual(false);

            // mark as not active
            gc.IsGiftCardActivated = false;
            (await _giftCardService.ValidateGiftCardAsync(gc, 1)).ShouldEqual(false);

            // again active
            gc.IsGiftCardActivated = true;
            (await _giftCardService.ValidateGiftCardAsync(gc, 1)).ShouldEqual(true);

            // add usage history record
            gc.GiftCardUsageHistory.Add(new GiftCardUsageHistory { UsedValue = 1000 });
            (await _giftCardService.ValidateGiftCardAsync(gc, 1)).ShouldEqual(false);
        }

        [Test]
        public async Task Can_calculate_giftCard_remainingAmountAsync()
        {
            var gc = new GiftCard()
            {
                Amount = 100
            };

            gc.GiftCardUsageHistory.Add(new GiftCardUsageHistory() { UsedValue = 30 });
            gc.GiftCardUsageHistory.Add(new GiftCardUsageHistory() { UsedValue = 20 });
            gc.GiftCardUsageHistory.Add(new GiftCardUsageHistory() { UsedValue = 5 });

            (await _giftCardService.GetRemainingAmountAsync(gc)).Amount.ShouldEqual(45);
        }
    }
}