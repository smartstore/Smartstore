using System.ComponentModel.DataAnnotations;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Checkout.Tax
{
    /// <summary>
    /// Represents a tax category
    /// </summary>
    [CacheableEntity]
    public partial class TaxCategory : EntityWithAttributes, IDisplayOrder
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