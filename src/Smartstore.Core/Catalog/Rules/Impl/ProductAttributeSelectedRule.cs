using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Rules;
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

            var parentId = expression.Descriptor.Metadata.Get("ParentId")?.Convert<int>() ?? 0;
            if (parentId == 0)
            {
                return false;
            }

            var selectedAttributes = await _productAttributeMaterializer.MaterializeProductVariantAttributesAsync(context.SelectedAttributes);
            var attribute = selectedAttributes.FirstOrDefault(x => x.ProductAttributeId == parentId);
            if (attribute == null)
            {
                return false;
            }

            var valueIds = context.SelectedAttributes.GetAttributeValues(attribute.Id)
                .Select(x => x.ToString())
                .Where(x => x.HasValue())
                .Select(x => x.ToInt())
                .Where(x => x != 0)
                .Distinct()
                .ToArray();
            if (valueIds.Length == 0)
            {
                return false;
            }

            var match = expression.HasListsMatch(valueIds);
            return match;
        }
    }
}
