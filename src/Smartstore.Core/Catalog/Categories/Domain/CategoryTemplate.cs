using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Catalog.Categories
{
    /// <summary>
    /// Represents a category template.
    /// </summary>
    [CacheableEntity]
    public partial class CategoryTemplate : EntityWithAttributes, IDisplayOrder
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
