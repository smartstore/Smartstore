using System.ComponentModel.DataAnnotations;
using Smartstore.Core.Localization;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Represents a price label
    /// </summary>
    [CacheableEntity]
    public partial class PriceLabel : EntityWithAttributes, ILocalizedEntity, IDisplayOrder
    {
        /// <summary>
        /// Gets or sets the short name that is usually displayed in listings, e.g. "MSRP", "Lowest".
        /// </summary>
        [Required, StringLength(16)]
        [LocalizedProperty]
        public string ShortName { get; set; }

        /// <summary>
        /// Gets or sets the optional name that is usually displayed in product detail, e.g. "Retail price", "Lowest recent price".
        /// </summary>
        [StringLength(50)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the optional description that is usually displayed in product detail tooltips.
        /// </summary>
        [StringLength(400)]
        [LocalizedProperty]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this label represents an MSRP price.
        /// </summary>
        public bool IsRetailPrice { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the label's short name should be displayed in product listings.
        /// </summary>
        public bool DisplayShortNameInLists { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}
