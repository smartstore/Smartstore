using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Catalog.Rules.Impl
{
    internal class ProductAttributeSelectedRule : IRule<AttributeRuleContext>
    {
        public Task<bool> MatchAsync(AttributeRuleContext context, RuleExpression expression)
        {
            if (context.SelectedValues.IsNullOrEmpty())
            {
                return Task.FromResult(false);
            }

            var attributeId = expression.Descriptor.Metadata.Get("Id")?.Convert<int>() ?? 0;
            if (attributeId == 0)
            {
                return Task.FromResult(false);
            }

            //var values = context.SelectedValues
            //    .Where(x => x.ProductVariantAttributeId == attributeId)
            //    .ToList();

            var valueIds = context.SelectedValues
                .Where(x => x.ProductVariantAttributeId == attributeId)
                .Select(x => x.Id)
                .ToArray();
            if (valueIds.Length == 0)
            {
                return Task.FromResult(false);
            }

            var match = expression.HasListsMatch(valueIds);
            return Task.FromResult(match);
        }
    }
}
