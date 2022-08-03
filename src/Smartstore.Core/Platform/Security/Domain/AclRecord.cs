using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Identity;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Security
{
    internal class AclRecordMap : IEntityTypeConfiguration<AclRecord>
    {
        public void Configure(EntityTypeBuilder<AclRecord> builder)
        {
            //builder
            //    .HasOne(x => x.CustomerRole)
            //    .WithMany()
            //    .HasForeignKey(x => x.CustomerRoleId)
            //    .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents an ACL record.
    /// </summary>
    [Index(nameof(EntityId), nameof(EntityName), Name = "IX_AclRecord_EntityId_EntityName")]
    [Index(nameof(IsIdle), Name = "IX_AclRecord_IsIdle")]
    [CacheableEntity]
    public partial class AclRecord : BaseEntity
    {
        public AclRecord()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private AclRecord(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the entity identifier.
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// Gets or sets the entity name.
        /// </summary>
        [Required, StringLength(400)]
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or sets the customer role identifier.
        /// </summary>
        public int CustomerRoleId { get; set; }

        private CustomerRole _customerRole;
        /// <summary>
        /// Gets or sets the customer role.
        /// </summary>
        public CustomerRole CustomerRole
        {
            get => _customerRole ?? LazyLoader.Load(this, ref _customerRole);
            set => _customerRole = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the entry is idle.
        /// </summary>
        /// <remarks>
        /// An entry is idle when it's related entity has been soft-deleted.
        /// </remarks>
        [Required]
        public bool IsIdle { get; set; }
    }
}
