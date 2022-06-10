using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Rules;
using Smartstore.Threading;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class CartSubtotalRule : IRule
    {
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOrderCalculationService _orderCalculationService;

        public CartSubtotalRule(IShoppingCartService shoppingCartService, IOrderCalculationService orderCalculationService)
        {
            _shoppingCartService = shoppingCartService;
            _orderCalculationService = orderCalculationService;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var sessionKey = context.SessionKey;
            var lockKey = "rule:cart:cartsubtotalrule:" + sessionKey.ToString();

            if (AsyncLock.IsLockHeld(lockKey))
            {
                //$"locked expression {expression.Id}: {lockKey}".Dump();
                return false;
            }

            // We must prevent the rule from indirectly calling itself. It would cause a stack overflow on cart page.
            using (await AsyncLock.KeyedAsync(lockKey))
            {
                var cart = await _shoppingCartService.GetCartAsync(context.Customer, ShoppingCartType.ShoppingCart, context.Store.Id);
                var subtotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart);

                // Subtotal is always calculated for working currency. No new money struct required here.
                // Currency values must be rounded here because otherwise unexpected results may occur.
                var cartSubtotal = subtotal.SubtotalWithoutDiscount.RoundedAmount;

                var result = expression.Operator.Match(cartSubtotal, expression.Value);
                //$"unlocked expression {expression.Id}: {lockKey}".Dump();
                return result;
            }
        }
    }
}
