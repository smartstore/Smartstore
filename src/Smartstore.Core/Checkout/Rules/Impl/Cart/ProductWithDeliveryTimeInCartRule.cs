using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class ProductWithDeliveryTimeInCartRule : IRule<CartRuleContext>
    {
        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var deliveryTimeIds = context.ShoppingCart.Items
                .Select(x => x.Item.Product.DeliveryTimeId ?? 0)
                .Where(x => x != 0)
                .Distinct()
                .ToArray();

            return Task.FromResult(expression.HasListsMatch(deliveryTimeIds));
        }
    }
}
