using System.ComponentModel.DataAnnotations;

namespace Smartstore.Core.Common
{
    /// <summary>
    /// Represents a generic attribute
    /// </summary>
    [Index(nameof(Key))]
    [Index(nameof(EntityId), nameof(KeyGroup), Name = "IX_GenericAttribute_EntityId_and_KeyGroup")]
    public partial class GenericAttribute : BaseEntity
    {
        /// <summary>
        /// Gets or sets the entity identifier
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// Gets or sets the key group
        /// </summary>
        [Required, StringLength(400)]
        public string KeyGroup { get; set; }

        /// <summary>
        /// Gets or sets the key
        /// </summary>
        [Required, StringLength(400)]
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        [Required, MaxLength]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the store identifier
        /// </summary>
        public int StoreId { get; set; }
    }
}
