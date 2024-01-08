using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Core.Catalog.Rules
{
    public class AttributeRuleContext(ProductVariantAttribute attribute, IList<ProductVariantAttributeValue> selectedValues)
    {
        public ProductVariantAttribute Attribute { get; } = Guard.NotNull(attribute);
        public IList<ProductVariantAttributeValue> SelectedValues { get; } = Guard.NotNull(selectedValues);
    }
}
