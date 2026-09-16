using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Localization;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;

namespace Smartstore.Core.Catalog.Attributes;

/// <summary>
/// Represents a product attribute.
/// </summary>
[Index(nameof(AllowFiltering), Name = "IX_AllowFiltering")]
[Index(nameof(DisplayOrder), Name = "IX_DisplayOrder")]
[LocalizedEntity("AllowFiltering")]
public partial class ProductAttribute : EntityWithAttributes, ILocalizedEntity, IDisplayOrder, ISearchAlias
{
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
    /// Gets or sets the effective swatch size.
    /// <c>null</c> means "use the global default" which is effectively a medium swatch size.
    /// </summary>
    public int? SwatchSizeId { get; set; }

    /// <summary>
    /// Gets or sets the swatch shape.
    /// <c>null</c> means "use the global default" which is effectively a square swatch shape.
    /// </summary>
    public int? SwatchShapeId { get; set; }

    /// <summary>
    /// Gets or sets the swatch aspect ratio.
    /// Controls the rendered swatch width-to-height ratio.
    /// Default is 1.0 (square).
    /// </summary>
    public decimal SwatchAspectRatio { get; set; } = 1m;

    /// <summary>
    /// Gets or sets a value indicating whether the value name is shown in the swatch.
    /// If <c>true</c>, shows the option text together with the rendered swatch.
    /// Default is <c>false</c> (only the swatch is shown).
    /// </summary>
    public bool ShowValueNameInSwatch { get; set; }

    /// <summary>
    /// Gets or sets which price information is displayed in the swatch.
    /// Default is 0 (no price information is shown).
    /// </summary>
    public int SwatchPriceDisplayId { get; set; }

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