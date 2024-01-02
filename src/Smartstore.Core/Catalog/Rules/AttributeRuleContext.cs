using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Core.Catalog.Rules
{
    public class AttributeRuleContext
    {
        public AttributeRuleContext(ProductVariantAttribute attribute, ProductVariantAttributeSelection selectedAttributes)
        {
            Guard.NotNull(attribute);
            Guard.NotNull(selectedAttributes);

            Attribute = attribute;
            SelectedAttributes = selectedAttributes;
        }

        public ProductVariantAttribute Attribute { get; }
        public ProductVariantAttributeSelection SelectedAttributes { get; set; }
    }
}
