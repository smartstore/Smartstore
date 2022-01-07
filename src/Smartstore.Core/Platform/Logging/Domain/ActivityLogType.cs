using System.ComponentModel.DataAnnotations;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Logging
{
    /// <summary>
    /// Represents an activity log type record
    /// </summary>
    [CacheableEntity]
    public partial class ActivityLogType : BaseEntity
    {
        /// <summary>
        /// Gets or sets the system keyword
        /// </summary>
        [Required, StringLength(100)]
        public string SystemKeyword { get; set; }

        /// <summary>
        /// Gets or sets the display name
        /// </summary>
        [Required, StringLength(200)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the activity log type is enabled
        /// </summary>
        public bool Enabled { get; set; }

    }
}