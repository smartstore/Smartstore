using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Rules
{
    public class AttributeRuleContext(
        Product product,
        ProductVariantAttribute attribute,
        IList<ProductVariantAttributeValue> selectedValues)
    {
        private int[] _selectedValueIds;

        public Product Product { get; } = Guard.NotNull(product);
        public ProductVariantAttribute Attribute { get; } = Guard.NotNull(attribute);
        public IList<ProductVariantAttributeValue> SelectedValues { get; } = Guard.NotNull(selectedValues);

        public int[] SelectedValueIds
        {
            get => _selectedValueIds ??= SelectedValues.Select(x => x.Id).ToArray();
        }
    }
}
