using System.ComponentModel.DataAnnotations;
using Smartstore.Core.Localization;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Common
{
    /// <summary>
    /// Represents a quantity unit
    /// </summary>
    [CacheableEntity]
    public partial class QuantityUnit : EntityWithAttributes, ILocalizedEntity, IDisplayOrder
    {
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [Required, StringLength(50)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name plural.
        /// </summary>
        [Required, StringLength(50)]
        [LocalizedProperty]
        public string NamePlural { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        [StringLength(50)]
        [LocalizedProperty]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the display locale
        /// </summary>
        [StringLength(50)]
        public string DisplayLocale { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the system global default quantity unit.
        /// </summary>
        public bool IsDefault { get; set; }
    }
}