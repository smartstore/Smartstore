using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class ProductInCartRule : IRule<CartRuleContext>
    {
        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var productIds = context.ShoppingCart.Items
                .Select(x => x.Item.ProductId)
                .Distinct()
                .ToArray();

            var match = expression.HasListsMatch(productIds);
            return Task.FromResult(match);
        }
    }
}
