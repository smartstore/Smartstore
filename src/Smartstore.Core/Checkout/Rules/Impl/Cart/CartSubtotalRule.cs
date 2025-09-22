using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Rules;
using Smartstore.Threading;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class CartSubtotalRule : IRule<CartRuleContext>
    {
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IRoundingHelper _roundingHelper;

        public CartSubtotalRule(IOrderCalculationService orderCalculationService, IRoundingHelper roundingHelper)
        {
            _orderCalculationService = orderCalculationService;
            _roundingHelper = roundingHelper;
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
                var cart = context.ShoppingCart;

                // Do not cache because otherwise subsequent calls of 'GetShoppingCartSubtotalAsync' may get
                // an incorrect result where this rule is not taken into account.
                var subtotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart, cache: false);

                // Currency values must be rounded here because otherwise unexpected results may occur.
                var roundedSubtotal = _roundingHelper.Round(subtotal.SubtotalWithoutDiscount.Amount);

                var result = expression.Operator.Match(roundedSubtotal, expression.Value);
                //$"unlocked expression {expression.Id}: {lockKey}".Dump();
                return result;
            }
        }
    }
}
