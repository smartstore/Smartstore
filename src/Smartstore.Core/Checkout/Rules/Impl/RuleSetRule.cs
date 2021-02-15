using System.Threading.Tasks;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    public class RuleSetRule : IRule
    {
        //private readonly IRuleFactory _ruleFactory;
        private readonly ICartRuleProvider _cartRuleProvider;

        public RuleSetRule(ICartRuleProvider cartRuleProvider)
        {
            _cartRuleProvider = cartRuleProvider;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var otherExpression = GetOtherExpression(expression);
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

        protected RuleExpression GetOtherExpression(RuleExpression expression)
        {
            var ruleSetId = expression.Value.Convert<int>();
            // TODO: (mg) (core) Complete RuleSetRule (IRuleFactory required).
            //var otherExpression = _ruleFactory.CreateExpressionGroup(ruleSetId, _cartRuleProvider) as RuleExpression;
            RuleExpression otherExpression = null;
            return otherExpression;
        }
    }
}
