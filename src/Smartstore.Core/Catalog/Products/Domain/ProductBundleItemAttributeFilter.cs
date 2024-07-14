﻿using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Represents a filter for a product bundle item.
    /// </summary>
    public partial class ProductBundleItemAttributeFilter : BaseEntity, ICloneable<ProductBundleItemAttributeFilter>
    {
        /// <summary>
        /// Gets or sets the product bundle item identifier.
        /// </summary>
        public int BundleItemId { get; set; }

        private ProductBundleItem _bundleItem;
        /// <summary>
        /// Gets or sets the bundle item.
        /// </summary>
        public ProductBundleItem BundleItem
        {
            get => _bundleItem ?? LazyLoader.Load(this, ref _bundleItem);
            set => _bundleItem = value;
        }

        /// <summary>
        /// Gets or sets the product attribute identifier.
        /// </summary>
        public int AttributeId { get; set; }

        /// <summary>
        /// Gets or sets the product attribute value identifier.
        /// </summary>
        public int AttributeValueId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the filtered value is pre-selected.
        /// </summary>
        public bool IsPreSelected { get; set; }

        /// <inheritdoc/>
        public ProductBundleItemAttributeFilter Clone()
        {
            var filter = new ProductBundleItemAttributeFilter
            {
                BundleItemId = BundleItemId,
                AttributeId = AttributeId,
                AttributeValueId = AttributeValueId,
                IsPreSelected = IsPreSelected
            };

            return filter;
        }

        object ICloneable.Clone() => Clone();
    }
}
