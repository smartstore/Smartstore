using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules
{
    public class CartCompositeRule : IRule<CartRuleContext>
    {
        private readonly RuleExpressionGroup _group;
        private readonly CartRuleProvider _cartRuleProvider;

        public CartCompositeRule(RuleExpressionGroup group, CartRuleProvider cartRuleProvider)
        {
            _group = group;
            _cartRuleProvider = cartRuleProvider;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var match = false;

            foreach (var expr in _group.Expressions.Cast<RuleExpression>())
            {
                if (expr.Descriptor is not CartRuleDescriptor descriptor)
                {
                    continue;
                }

                var processor = _cartRuleProvider.GetProcessor(expr);

                match = await processor.MatchAsync(context, expr);

                if (!match && _group.LogicalOperator == LogicalRuleOperator.And)
                {
                    break;
                }

                if (match && _group.LogicalOperator == LogicalRuleOperator.Or)
                {
                    break;
                }
            }

            return match;
        }
    }
}
