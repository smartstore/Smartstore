using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl;

/// <summary>
/// Checks whether all the products in the shopping cart have the specified delivery time assigned to them.
/// </summary>
internal class AllProductsWithDeliveryTimeInCartRule : IRule<CartRuleContext>
{
    public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
    {
        var products = context.ShoppingCart.Items.Select(x => x.Item.Product);

        foreach (var product in products)
        {
            if (!product.IsShippingEnabled || !expression.HasListMatch(product.DeliveryTimeId ?? 0))
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }
}