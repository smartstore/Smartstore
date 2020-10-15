using System;
using System.ComponentModel.DataAnnotations;
using Smartstore.Domain;

namespace Smartstore.Core.Tax
{
    /// <summary>
    /// Represents a tax category
    /// </summary>
    public partial class TaxCategory : BaseEntity, IDisplayOrder
    {
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [Required, StringLength(400)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}
