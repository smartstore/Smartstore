using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Rules;
using Smartstore.Threading;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class CartTotalRule : IRule
    {
        private readonly IOrderCalculationService _orderCalculationService;

        public CartTotalRule(IOrderCalculationService orderCalculationService)
        {
            _orderCalculationService = orderCalculationService;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var sessionKey = context.SessionKey;
            var lockKey = "rule:cart:carttotalrule:" + sessionKey.ToString();

            if (AsyncLock.IsLockHeld(lockKey))
            {
                return false;
            }

            // We must prevent the rule from indirectly calling itself. It would cause a stack overflow on cart page.
            using (await AsyncLock.KeyedAsync(lockKey))
            {
                var cart = context.ShoppingCart;
                var cartTotalResult = await _orderCalculationService.GetShoppingCartTotalAsync(cart);

                // Currency values must be rounded here because otherwise unexpected results may occur.
                var cartTotal = cartTotalResult.Total?.RoundedAmount ?? decimal.Zero;

                var result = expression.Operator.Match(cartTotal, expression.Value);
                return result;
            }
        }
    }
}
