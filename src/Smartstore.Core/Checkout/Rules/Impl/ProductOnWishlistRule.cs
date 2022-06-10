using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class ProductOnWishlistRule : IRule
    {
        private readonly IShoppingCartService _shoppingCartService;

        public ProductOnWishlistRule(IShoppingCartService shoppingCartService)
        {
            _shoppingCartService = shoppingCartService;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var wishlist = await _shoppingCartService.GetCartAsync(context.Customer, ShoppingCartType.Wishlist, context.Store.Id);
            var productIds = wishlist.Items
                .Select(x => x.Item.ProductId)
                .Distinct()
                .ToArray();

            var match = expression.HasListsMatch(productIds);
            return match;
        }
    }
}
