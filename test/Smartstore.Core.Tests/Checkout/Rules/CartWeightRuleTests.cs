using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Checkout.Rules.Impl;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Identity;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Tests.Checkout.Rules;

[TestFixture]
public class CartWeightRuleTests
{
    [Test]
    public async Task Uses_uncached_cart_weight()
    {
        var cart = new ShoppingCart(new Customer { Id = 1 }, 1, []);
        var shippingService = new Mock<IShippingService>();
        shippingService
            .Setup(x => x.GetCartTotalWeightAsync(cart, true, false))
            .ReturnsAsync(10m);

        var rule = new CartWeightRule(shippingService.Object);
        var context = new CartRuleContext(() => 1) { ShoppingCart = cart };
        var expression = new RuleExpression
        {
            Operator = RuleOperator.GreaterThan,
            Value = 5m
        };

        var result = await rule.MatchAsync(context, expression);

        Assert.That(result, Is.True);
        shippingService.Verify(x => x.GetCartTotalWeightAsync(cart, true, false), Times.Once);
    }
}