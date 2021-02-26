using System.Threading.Tasks;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common;
using Smartstore.Core.Rules;
using Smartstore.Threading;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class CartSubtotalRule : IRule
    {
        private readonly IShoppingCartService _shoppingCartService;

        public CartSubtotalRule(IShoppingCartService shoppingCartService)
        {
            _shoppingCartService = shoppingCartService;
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
                var cart = await _shoppingCartService.GetCartItemsAsync(context.Customer, ShoppingCartType.ShoppingCart, context.Store.Id);

                // TODO: (mg) (core) Complete CartSubtotalRule (IOrderTotalCalculationService required).
                //await _orderTotalCalculationService.GetShoppingCartSubTotalAsync(cart, out _, out _, out var cartSubtotal, out _);
                var cartSubtotal = decimal.Zero;

                // Currency values must be rounded, otherwise unexpected results may occur.
                var money = new Money(cartSubtotal, context.WorkContext.WorkingCurrency);
                cartSubtotal = money.RoundedAmount;

                var result = expression.Operator.Match(cartSubtotal, expression.Value);
                //$"unlocked expression {expression.Id}: {lockKey}".Dump();
                return result;
            }
        }
    }
}
