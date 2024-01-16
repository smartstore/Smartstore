using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class ProductOnWishlistRule : IRule<CartRuleContext>
    {
        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var wishlist = await context.ShoppingCartService.GetCartAsync(context.Customer, ShoppingCartType.Wishlist, context.Store.Id);
            var productIds = wishlist.Items
                .Select(x => x.Item.ProductId)
                .Distinct()
                .ToArray();

            var match = expression.HasListsMatch(productIds);
            return match;
        }
    }
}
