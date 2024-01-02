using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Catalog.Rules.Impl
{
    internal class ProductAttributeSelectedRule(IProductAttributeMaterializer productAttributeMaterializer) : IRule<AttributeRuleContext>
    {
        private readonly IProductAttributeMaterializer _productAttributeMaterializer = productAttributeMaterializer;

        public async Task<bool> MatchAsync(AttributeRuleContext context, RuleExpression expression)
        {
            if (context.SelectedAttributes.IsNullOrEmpty())
            {
                return false;
            }

            var attributeId = expression.Descriptor.Metadata.Get("ParentId").Convert<int>();
            if (attributeId == 0)
            {
                return false;
            }

            //context.SelectedAttributes.GetAttributeValues(ProductVariantAttribute.Id)
            var selectedAttributes = await _productAttributeMaterializer.MaterializeProductVariantAttributesAsync(context.SelectedAttributes);
            if (selectedAttributes.Count == 0)
            {
                return false;
            }

            await Task.Delay(10);
            throw new NotImplementedException();

            //var match = expression.HasListsMatch(valueIds);
            //return Task.FromResult(match);
        }
    }
}
