using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class BillingCountryRule : IRule<CartRuleContext>
    {
        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var match = expression.HasListMatch(context.Customer?.BillingAddress?.CountryId ?? 0);
            return Task.FromResult(match);
        }
    }
}
