using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class ShippingEuRule : IRule<CartRuleContext>
    {
        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var subjectToVat = context.Customer?.ShippingAddress?.Country?.SubjectToVat ?? false;
            var match = expression.Operator.Match(subjectToVat, expression.Value);

            return Task.FromResult(match);
        }
    }
}
