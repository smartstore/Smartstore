using Smartstore.Core.Rules;

namespace Smartstore.Core.Catalog.Rules
{
    public class AttributeCompositeRule : IRule<AttributeRuleContext>
    {
        private readonly RuleExpressionGroup _group;
        private readonly AttributeRuleProvider _attributeRuleProvider;

        public AttributeCompositeRule(RuleExpressionGroup group, AttributeRuleProvider attributeRuleProvider)
        {
            _group = group;
            _attributeRuleProvider = attributeRuleProvider;
        }

        public async Task<bool> MatchAsync(AttributeRuleContext context, RuleExpression expression)
        {
            var match = false;

            foreach (var expr in _group.Expressions.Cast<RuleExpression>())
            {
                if (expr.Descriptor is not AttributeRuleDescriptor descriptor)
                {
                    continue;
                }

                var processor = _attributeRuleProvider.GetProcessor(expr);

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
