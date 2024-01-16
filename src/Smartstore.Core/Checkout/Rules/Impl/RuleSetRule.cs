using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    public class RuleSetRule : IRule<CartRuleContext>
    {
        private readonly IRuleService _ruleService;
        private readonly ICartRuleProvider _cartRuleProvider;

        public RuleSetRule(IRuleService ruleService, IRuleProviderFactory ruleProviderFactory)
        {
            _ruleService = ruleService;
            _cartRuleProvider = ruleProviderFactory.GetProvider<ICartRuleProvider>(RuleScope.Cart);
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var otherExpression = await GetOtherExpressionAsync(expression);
            if (otherExpression == null)
            {
                // Skip\ignore expression.
                return true;
            }

            var otherRule = _cartRuleProvider.GetProcessor(otherExpression);
            //var otherMatch = otherRule.Match(context, otherExpression);

            //return expression.Operator.Match(otherMatch, true);

            if (expression.Operator == RuleOperator.IsEqualTo)
            {
                return await otherRule.MatchAsync(context, otherExpression);
            }
            if (expression.Operator == RuleOperator.IsNotEqualTo)
            {
                return !await otherRule.MatchAsync(context, otherExpression);
            }

            throw new InvalidRuleOperatorException(expression);
        }

        protected async Task<RuleExpression> GetOtherExpressionAsync(RuleExpression expression)
        {
            var ruleSetId = expression.Value.Convert<int>();
            var otherExpression = await _ruleService.CreateExpressionGroupAsync(ruleSetId, _cartRuleProvider) as RuleExpression;

            return otherExpression;
        }
    }
}
