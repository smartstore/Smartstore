using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Core.Catalog.Rules
{
    public partial class AttributeRuleProviderContext(
        ProductVariantAttribute attribute,
        List<ProductVariantAttribute> allAttributes)
    {
        public ProductVariantAttribute Attribute { get; } = Guard.NotNull(attribute);
        public List<ProductVariantAttribute> AllAttributes { get; } = Guard.NotNull(allAttributes);
    }
}
