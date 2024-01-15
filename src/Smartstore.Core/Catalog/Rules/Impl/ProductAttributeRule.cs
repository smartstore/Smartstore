using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Catalog.Rules.Impl
{
    internal class ProductAttributeRule : IRule<AttributeRuleContext>
    {
        public Task<bool> MatchAsync(AttributeRuleContext context, RuleExpression expression)
        {
            var match = false;
            var ruleAttributeId = expression.Descriptor.Metadata.Get("ParentId")?.Convert<int>() ?? 0;
            if (ruleAttributeId != 0)
            {
                var valueIds = context.SelectedValues
                    .Where(x => x.ProductVariantAttributeId == ruleAttributeId)
                    .Select(x => x.Id)
                    .ToArray();

                if (valueIds.Length > 0)
                {
                    match = expression.Descriptor.IsComparingSequences
                        ? expression.HasListsMatch(valueIds)
                        : expression.HasListMatch(valueIds[0]);

                    //$"- match:{match,-5} sequence:{expression.Descriptor.IsComparingSequences,-5} check:{context.Attribute.Id,-6} condition:{ruleAttributeId,-6} selected:{string.Join(',', valueIds)}".Dump();
                }
            }

            return Task.FromResult(match);
        }
    }
}
