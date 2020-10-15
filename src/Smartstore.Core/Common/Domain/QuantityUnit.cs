using System.ComponentModel.DataAnnotations;
using Smartstore.Core.Content.Localization;
using Smartstore.Domain;

namespace Smartstore.Core.Common
{
    /// <summary>
    /// Represents a quantity unit
    /// </summary>
    public partial class QuantityUnit : BaseEntity, ILocalizedEntity, IDisplayOrder
    {
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [Required, StringLength(50)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name plural.
        /// </summary>
        [Required, StringLength(50)]
        public string NamePlural { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        [StringLength(50)]
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
        /// Gets or sets the default quantity unit
        /// </summary>
        public bool IsDefault { get; set; }
    }
}