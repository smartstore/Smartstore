using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Checkout.Rules.Impl;
using Smartstore.Core.Identity;
using Smartstore.Core.Rules;
using Smartstore.Core.Stores;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Checkout.Rules;

[TestFixture]
public class CartRuleTests : ServiceTestBase
{
    [TestCase(false, false, true)]
    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    [TestCase(null, true, false)]
    public async Task Matches_discount_rule(bool? hasValidDiscount, bool value, bool expectedResult)
    {
        const string couponCode = "discount-code";
        var customer = CreateTestCustomer();
        var store = new Store { Id = 2 };
        var discount = new Discount { Id = 3, RequiresCouponCode = true, CouponCode = couponCode };
        var validationFlags = DiscountValidationFlags.All & ~DiscountValidationFlags.CartRules;
        var discountService = new Mock<IDiscountService>();

        if (hasValidDiscount != null)
        {
            customer.GenericAttributes.DiscountCouponCode = couponCode;

            discountService
                .Setup(x => x.GetAllDiscountsAsync(null, couponCode, true))
                .ReturnsAsync([discount]);
            discountService
                .Setup(x => x.IsDiscountValidAsync(discount, customer, couponCode, store, validationFlags))
                .ReturnsAsync(hasValidDiscount.Value);
        }

        var rule = new DiscountRule(discountService.Object);
        var context = new CartRuleContext(() => 0)
        {
            Customer = customer,
            Store = store
        };
        var expression = new RuleExpression
        {
            Operator = RuleOperator.IsEqualTo,
            Value = value
        };

        var result = await rule.MatchAsync(context, expression);
        result.ShouldEqual(expectedResult);

        if (hasValidDiscount != null)
        {
            discountService.Verify(x => x.GetAllDiscountsAsync(null, couponCode, true), Times.Once);
            discountService.Verify(x => x.IsDiscountValidAsync(discount, customer, couponCode, store, validationFlags), Times.Once);
        }
        else
        {
            discountService.VerifyNoOtherCalls();
        }
    }

    [TestCase(false, false, true)]
    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    public async Task Matches_gift_card_rule(bool hasValidGiftCard, bool value, bool expectedResult)
    {
        var customer = new Customer { Id = 1 };
        var store = new Store { Id = 2 };   
        var giftCards = hasValidGiftCard
            ? new List<AppliedGiftCard> { new() }
            : [];

        var giftCardService = new Mock<IGiftCardService>();
        giftCardService
            .Setup(x => x.GetValidGiftCardsAsync(store.Id, customer))
            .ReturnsAsync(giftCards);

        var rule = new GiftCardRule(giftCardService.Object);
        var context = new CartRuleContext(() => 0)
        {
            Customer = customer,
            Store = store
        };
        var expression = new RuleExpression
        {
            Operator = RuleOperator.IsEqualTo,
            Value = value
        };

        var result = await rule.MatchAsync(context, expression);

        result.ShouldEqual(expectedResult);
        giftCardService.Verify(x => x.GetValidGiftCardsAsync(store.Id, customer), Times.Once);
    }

    private Customer CreateTestCustomer()
    {
        var customer = new Customer { Id = 1 };
        var query = DbContext.GenericAttributes
            .Where(x => x.KeyGroup == nameof(Customer) && x.EntityId == customer.Id);

        customer.SetGenericAttributes(new(query, nameof(Customer), customer.Id, 0));

        return customer;
    }
}
