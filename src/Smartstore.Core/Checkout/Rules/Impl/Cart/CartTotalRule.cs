using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Rules;
using Smartstore.Threading;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class CartTotalRule : IRule<CartRuleContext>
    {
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IRoundingHelper _roundingHelper;

        public CartTotalRule(IOrderCalculationService orderCalculationService, IRoundingHelper roundingHelper)
        {
            _orderCalculationService = orderCalculationService;
            _roundingHelper = roundingHelper;
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

                // Do not cache because otherwise subsequent calls of 'GetShoppingCartTotalAsync' may get
                // an incorrect result where this rule is not taken into account.
                var cartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart, cache: false);

                // Currency values must be rounded here because otherwise unexpected results may occur.
                var roundedTotal = _roundingHelper.Round(cartTotal.Total?.Amount ?? decimal.Zero);

                var result = expression.Operator.Match(roundedTotal, expression.Value);
                return result;
            }
        }
    }
}
