using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Smartstore.Domain;

namespace Smartstore.Core.Stores
{
    /// <summary>
    /// Represents a store mapping record
    /// </summary>
    [Index(nameof(EntityId), nameof(EntityName), Name = "IX_StoreMapping_EntityId_EntityName")]
    public partial class StoreMapping : EntityWithAttributes
    {
        /// <summary>
        /// Gets or sets the entity identifier
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// Gets or sets the entity name
        /// </summary>
        [Required, StringLength(400)]
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or sets the store identifier
        /// </summary>
        public int StoreId { get; set; }
    }
}
