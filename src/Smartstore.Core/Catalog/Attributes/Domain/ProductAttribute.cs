using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using Smartstore.Core.Localization;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;

namespace Smartstore.Core.Catalog.Attributes
{
    /// <summary>
    /// Represents a product attribute.
    /// </summary>
    [Index(nameof(AllowFiltering), Name = "IX_AllowFiltering")]
    [Index(nameof(DisplayOrder), Name = "IX_DisplayOrder")]
    [LocalizedEntity("AllowFiltering")]
    public partial class ProductAttribute : EntityWithAttributes, ILocalizedEntity, IDisplayOrder, ISearchAlias
    {
        public ProductAttribute()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ProductAttribute(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the attribute name.
        /// </summary>
        /// <example>Color</example>
        [Required, StringLength(4000)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [StringLength(4000)]
        [LocalizedProperty]
        public string Description { get; set; }

        /// <inheritdoc/>
        [StringLength(100)]
        [LocalizedProperty]
        public string Alias { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the attribute can be filtered.
        /// </summary>
        public bool AllowFiltering { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the facet template hint.
        /// Only effective in accordance with MegaSearchPlus module.
        /// </summary>
        public FacetTemplateHint FacetTemplateHint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether option names should be included in the search index.
        /// Only effective in accordance with MegaSearchPlus module.
        /// </summary>
        public bool IndexOptionNames { get; set; }

        /// <summary>
        /// Gets or sets optional export mappings.
        /// </summary>
        [MaxLength]
        public string ExportMappings { get; set; }

        private ICollection<ProductAttributeOptionsSet> _productAttributeOptionsSets;
        /// <summary>
        /// Gets or sets the options sets.
        /// </summary>
        public ICollection<ProductAttributeOptionsSet> ProductAttributeOptionsSets
        {
            get => _productAttributeOptionsSets ?? LazyLoader.Load(this, ref _productAttributeOptionsSets) ?? (_productAttributeOptionsSets ??= new HashSet<ProductAttributeOptionsSet>());
            protected set => _productAttributeOptionsSets = value;
        }
    }
}
