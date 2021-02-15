using System.Threading.Tasks;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common;
using Smartstore.Core.Rules;
using Smartstore.Threading;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    public class CartTotalRule : IRule
    {
        private readonly IShoppingCartService _shoppingCartService;

        public CartTotalRule(IShoppingCartService shoppingCartService)
        {
            _shoppingCartService = shoppingCartService;
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
                var cart = await _shoppingCartService.GetCartItemsAsync(context.Customer, ShoppingCartType.ShoppingCart, context.Store.Id);

                // TODO: (mg) (core) Complete CartTotalRule (IOrderTotalCalculationService required).
                //var cartTotal = ((decimal?)_orderTotalCalculationService.GetShoppingCartTotal(cart)) ?? decimal.Zero;
                var cartTotal = decimal.Zero;

                // Currency values must be rounded, otherwise unexpected results may occur.
                var money = new Money(cartTotal, context.WorkContext.WorkingCurrency);
                cartTotal = money.RoundedAmount;

                var result = expression.Operator.Match(cartTotal, expression.Value);
                return result;
            }
        }
    }
}
