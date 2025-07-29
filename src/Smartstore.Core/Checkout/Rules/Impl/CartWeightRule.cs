using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class CartWeightRule(IShippingService shippingService) : IRule<CartRuleContext>
    {
        private readonly IShippingService _shippingService = shippingService;

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var weight = await _shippingService.GetCartTotalWeightAsync(context.ShoppingCart);
            return expression.Operator.Match(weight, expression.Value);
        }
    }
}
