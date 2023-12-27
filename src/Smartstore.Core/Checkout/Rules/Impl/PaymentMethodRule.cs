using Smartstore.Core.Identity;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class PaymentMethodRule : IRule<CartRuleContext>
    {
        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var paymentMethod = context.Customer.GenericAttributes.Get<string>(SystemCustomerAttributeNames.SelectedPaymentMethod, context.Store.Id);
            var match = expression.HasListMatch(paymentMethod.NullEmpty(), StringComparer.InvariantCultureIgnoreCase);

            return Task.FromResult(match);
        }
    }
}
