using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class ShippingCompanyRule : IRule<CartRuleContext>
    {
        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var hasCompany = context.Customer?.ShippingAddress?.Company?.HasValue() ?? false;
            var match = expression.Operator.Match(hasCompany, expression.Value);

            return Task.FromResult(match);
        }
    }
}
