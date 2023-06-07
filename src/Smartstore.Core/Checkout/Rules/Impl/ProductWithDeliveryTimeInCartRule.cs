using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class ProductWithDeliveryTimeInCartRule : IRule
    {
        private readonly IShoppingCartService _shoppingCartService;

        public ProductWithDeliveryTimeInCartRule(IShoppingCartService shoppingCartService)
        {
            _shoppingCartService = shoppingCartService;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var cart = await _shoppingCartService.GetCartAsync(context.Customer, ShoppingCartType.ShoppingCart, context.Store.Id);
            var deliveryTimeIds = cart.Items
                .Select(x => x.Item.Product.DeliveryTimeId ?? 0)
                .Where(x => x != 0)
                .Distinct()
                .ToArray();

            return expression.HasListsMatch(deliveryTimeIds);
        }
    }
}
