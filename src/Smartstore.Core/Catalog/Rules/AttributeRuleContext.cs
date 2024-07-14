﻿using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Rules
{
    public class AttributeRuleContext(
        Product product,
        ProductVariantAttribute attribute,
        IList<ProductVariantAttributeValue> selectedValues)
    {
        private int[] _selectedValueIds;

        /// <summary>
        /// Gets the product belonging to <see cref="Attribute"/>.
        /// </summary>
        public Product Product { get; } = Guard.NotNull(product);

        /// <summary>
        /// Gets the current product attribute to be checked.
        /// </summary>
        public ProductVariantAttribute Attribute { get; } = Guard.NotNull(attribute);

        /// <summary>
        /// Gets the values of the currently selected product attributes.
        /// </summary>
        public IList<ProductVariantAttributeValue> SelectedValues { get; } = Guard.NotNull(selectedValues);

        /// <summary>
        /// Gets the identifiers of the selected product attribute values.
        /// Only includes list-type attributes (<see cref="ProductVariantAttribute.IsListTypeAttribute"/>).
        /// </summary>
        public int[] SelectedValueIds => _selectedValueIds ??= SelectedValues.Select(x => x.Id).ToArray();
    }
}
