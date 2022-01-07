using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class CartProductCountRule : IRule
    {
        private readonly IShoppingCartService _shoppingCartService;

        public CartProductCountRule(IShoppingCartService shoppingCartService)
        {
            _shoppingCartService = shoppingCartService;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var cart = await _shoppingCartService.GetCartAsync(context.Customer, ShoppingCartType.ShoppingCart, context.Store.Id);
            var productCount = cart.GetTotalQuantity();

            return expression.Operator.Match(productCount, expression.Value);
        }
    }
}
