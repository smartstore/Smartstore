using System.ComponentModel.DataAnnotations;
using Smartstore.Data.Caching;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.DataExchange
{
    /// <summary>
    /// Holds info about a synchronization operation with an external system.
    /// </summary>
    [Hookable(false)]
    [CacheableEntity(NeverCache = true)]
    [Index(nameof(EntityId), nameof(EntityName), nameof(ContextName), Name = "IX_SyncMapping_ByEntity", IsUnique = true)]
    [Index(nameof(SourceKey), nameof(EntityName), nameof(ContextName), Name = "IX_SyncMapping_BySource", IsUnique = true)]
    public partial class SyncMapping : BaseEntity
    {
        /// <summary>
        /// Gets or sets the entity identifier in Smartstore.
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// Gets or sets the entity's key in the external application.
        /// </summary>
        [Required, StringLength(150)]
        public string SourceKey { get; set; }

        /// <summary>
        /// Gets or sets a name representing the entity type.
        /// </summary>
        [Required, StringLength(100)]
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or sets a name for the external application.
        /// </summary>
        [Required, StringLength(100)]
        public string ContextName { get; set; }

        /// <summary>
        /// Gets or sets an optional content hash reflecting the source model at the time of last sync.
        /// </summary>
        [StringLength(40)]
        public string SourceHash { get; set; }

        /// <summary>
        /// Gets or sets a custom integer value.
        /// </summary>
        public int? CustomInt { get; set; }

        /// <summary>
        /// Gets or sets a custom string value.
        /// </summary>
        [MaxLength]
        public string CustomString { get; set; }

        /// <summary>
        /// Gets or sets a custom bool value.
        /// </summary>
        public bool? CustomBool { get; set; }

        /// <summary>
        /// Gets or sets the date of the last sync operation.
        /// </summary>
        public DateTime SyncedOnUtc { get; set; } = DateTime.UtcNow;
    }
}
