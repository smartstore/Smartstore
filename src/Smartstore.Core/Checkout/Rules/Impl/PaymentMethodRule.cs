using System;
using System.Threading.Tasks;
using Smartstore.Core.Identity;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    public class PaymentMethodRule : IRule
    {
        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var paymentMethod = context.Customer.GenericAttributes.Get<string>(SystemCustomerAttributeNames.SelectedPaymentMethod, context.Store.Id);
            var match = expression.HasListMatch(paymentMethod.NullEmpty(), StringComparer.InvariantCultureIgnoreCase);

            return Task.FromResult(match);
        }
    }
}
