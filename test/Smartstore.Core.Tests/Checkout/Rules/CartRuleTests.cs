using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Checkout.Rules.Impl;
using Smartstore.Core.Identity;
using Smartstore.Core.Rules;
using Smartstore.Core.Stores;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Checkout.Rules;

[TestFixture]
public class CartRuleTests
{
    [TestCase(false, false, true)]
    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    public async Task Matches_gift_card_rule(bool hasGiftCardApplied, bool expectedValue, bool expectedResult)
    {
        var customer = new Customer { Id = 1 };
        var store = new Store { Id = 2 };   
        var giftCards = hasGiftCardApplied
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
            Value = expectedValue
        };

        var result = await rule.MatchAsync(context, expression);

        result.ShouldEqual(expectedResult);
        giftCardService.Verify(x => x.GetValidGiftCardsAsync(store.Id, customer), Times.Once);
    }
}
