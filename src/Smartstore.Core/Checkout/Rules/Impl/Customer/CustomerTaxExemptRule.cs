using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class CustomerTaxExemptRule : IRule<CartRuleContext>
    {
        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var customer = context.Customer;
            var isTaxExempt = customer != null && !customer.IsSystemAccount && customer.IsTaxExempt;
            var match = expression.Operator.Match(isTaxExempt, expression.Value);

            return Task.FromResult(match);
        }
    }
}
