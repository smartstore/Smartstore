using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Smartstore.Core.Content.Localization;
using Smartstore.Domain;

namespace Smartstore.Core.Common
{
    /// <summary>
    /// Represents a measure dimension
    /// </summary>
    public partial class MeasureDimension : BaseEntity, ILocalizedEntity, IDisplayOrder
    {
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [Required, StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the system keyword
        /// </summary>
        [Required, StringLength(100)]
        public string SystemKeyword { get; set; }

        /// <summary>
        /// Gets or sets the ratio
        /// </summary>
        [Column(TypeName = "decimal(18,8)")]
        public decimal Ratio { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}