using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Catalog.Rules
{
    public class NullAttributeRuleProvider : IRuleProvider, IAttributeRuleProvider
    {
        public RuleScope Scope => RuleScope.ProductAttribute;

        public Task<IRuleExpressionGroup> CreateExpressionGroupAsync(ProductVariantAttribute attribute, bool includeHidden)
            => Task.FromResult<IRuleExpressionGroup>(null);

        public IRule<AttributeRuleContext> GetProcessor(RuleExpression expression)
            => null;

        public Task<IRuleExpression> VisitRuleAsync(RuleEntity rule)
            => Task.FromResult<IRuleExpression>(null);

        public IRuleExpressionGroup VisitRuleSet(RuleSetEntity ruleSet)
            => null;

        public Task<ProductVariantAttribute[]> GetInactiveAttributesAsync(Product product, ProductVariantAttributeSelection selection, LogicalRuleOperator logicalOperator)
            => Task.FromResult(Array.Empty<ProductVariantAttribute>());

        public Task<bool> IsAttributeActiveAsync(AttributeRuleContext context, LogicalRuleOperator logicalOperator)
            => Task.FromResult(true);

        public Task<RuleDescriptorCollection> GetRuleDescriptorsAsync()
            => Task.FromResult(new RuleDescriptorCollection());
    }
}
