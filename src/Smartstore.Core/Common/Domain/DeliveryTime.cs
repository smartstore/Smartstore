using System.ComponentModel.DataAnnotations;
using Smartstore.Core.Localization;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Common
{
    /// <summary>
    /// Represents a delivery time
    /// </summary>
    [CacheableEntity]
    public partial class DeliveryTime : EntityWithAttributes, ILocalizedEntity, IDisplayOrder
    {
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [Required, StringLength(50)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the hex value
        /// </summary>
        [Required, StringLength(50)]
        public string ColorHexValue { get; set; }

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
        /// Gets or sets a value indicating whether this is the system global default delivery time.
        /// </summary>
        public bool? IsDefault { get; set; }

        /// <summary>
        /// Specifies the earliest time of delivery in days.
        /// </summary>
        public int? MinDays { get; set; }

        /// <summary>
        /// Specifies the latest time of delivery in days.
        /// </summary>
        public int? MaxDays { get; set; }
    }
}