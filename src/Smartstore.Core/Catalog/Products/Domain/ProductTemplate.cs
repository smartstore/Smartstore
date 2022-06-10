using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Represents a product template.
    /// </summary>
    [CacheableEntity]
    public partial class ProductTemplate : EntityWithAttributes, IDisplayOrder
    {
        /// <summary>
        /// Gets or sets the template name.
        /// </summary>
        [Required, StringLength(400)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the view path.
        /// </summary>
        [Required, StringLength(400)]
        public string ViewPath { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}
