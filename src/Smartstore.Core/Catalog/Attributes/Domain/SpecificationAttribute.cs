using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using Smartstore.Core.Localization;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;

namespace Smartstore.Core.Catalog.Attributes
{
    /// <summary>
    /// Represents a specification attribute.
    /// </summary>
    [Index(nameof(AllowFiltering), Name = "IX_AllowFiltering")]
    [Index(nameof(Essential), Name = "IX_EssentialAttribute")]
    [LocalizedEntity("ShowOnProductPage or AllowFiltering")]
    public partial class SpecificationAttribute : EntityWithAttributes, ILocalizedEntity, IDisplayOrder, ISearchAlias
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required, StringLength(4000)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <inheritdoc/>
        [StringLength(30)]
        [LocalizedProperty]
        public string Alias { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the specification attribute is essential.
        /// Essential attributes are also displayed in the checkout (e.g. on the order confirmation page).
        /// </summary>
        public bool Essential { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the specification attribute will be shown on the product page.
        /// </summary>
        public bool ShowOnProductPage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the specification attribute can be filtered.
        /// Only effective in accordance with MegaSearchPlus module.
        /// </summary>
        public bool AllowFiltering { get; set; }

        /// <summary>
        /// Gets or sets the sorting of facets.
        /// Only effective in accordance with MegaSearchPlus module.
        /// </summary>
        public FacetSorting FacetSorting { get; set; }

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

        private ICollection<SpecificationAttributeOption> _specificationAttributeOptions;
        /// <summary>
        /// Gets or sets the specification attribute options.
        /// </summary>
        public ICollection<SpecificationAttributeOption> SpecificationAttributeOptions
        {
            get => _specificationAttributeOptions ?? LazyLoader.Load(this, ref _specificationAttributeOptions) ?? (_specificationAttributeOptions ??= new HashSet<SpecificationAttributeOption>());
            protected set => _specificationAttributeOptions = value;
        }
    }
}