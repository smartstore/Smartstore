using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Core.Catalog.Rules
{
    public class AttributeRuleContext
    {
        public AttributeRuleContext(ProductVariantAttribute attribute, ProductVariantQuery query)
        {
            Guard.NotNull(attribute);
            Guard.NotNull(query);

            Attribute = attribute;
            Query = query;
        }

        public ProductVariantAttribute Attribute { get; }
        public ProductVariantQuery Query { get; }
    }
}
