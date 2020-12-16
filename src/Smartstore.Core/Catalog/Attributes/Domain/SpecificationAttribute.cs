using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using Smartstore.Core.Localization;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;
using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Attributes
{
    /// <summary>
    /// Represents a specification attribute.
    /// </summary>
    [Index(nameof(AllowFiltering), Name = "IX_AllowFiltering")]
    public partial class SpecificationAttribute : BaseEntity, ILocalizedEntity, IDisplayOrder, ISearchAlias
    {
        private readonly ILazyLoader _lazyLoader;

        public SpecificationAttribute()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private SpecificationAttribute(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required, StringLength(4000)]
        public string Name { get; set; }

        /// <inheritdoc/>
        [StringLength(30)]
        public string Alias { get; set; }

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
        /// Only effective in accordance with MegaSearchPlus plugin.
        /// </summary>
        public bool AllowFiltering { get; set; }

        /// <summary>
        /// Gets or sets the sorting of facets.
        /// Only effective in accordance with MegaSearchPlus plugin.
        /// </summary>
        public FacetSorting FacetSorting { get; set; }

        /// <summary>
        /// Gets or sets the facet template hint.
        /// Only effective in accordance with MegaSearchPlus plugin.
        /// </summary>
        public FacetTemplateHint FacetTemplateHint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether option names should be included in the search index.
        /// Only effective in accordance with MegaSearchPlus plugin.
        /// </summary>
        public bool IndexOptionNames { get; set; }

        private ICollection<SpecificationAttributeOption> _specificationAttributeOptions;
        /// <summary>
        /// Gets or sets the specification attribute options.
        /// </summary>
        public ICollection<SpecificationAttributeOption> SpecificationAttributeOptions
        {
            get => _lazyLoader?.Load(this, ref _specificationAttributeOptions) ?? (_specificationAttributeOptions ??= new HashSet<SpecificationAttributeOption>());
            protected set => _specificationAttributeOptions = value;
        }
    }
}