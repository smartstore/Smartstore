using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Rules;
using Smartstore.Core.Security;

namespace Smartstore.Core.Identity
{
    internal class CustomerRoleMap : IEntityTypeConfiguration<CustomerRole>
    {
        public void Configure(EntityTypeBuilder<CustomerRole> builder)
        {
            builder.Property(c => c.OrderTotalMinimum).HasPrecision(18, 2);
            builder.Property(c => c.OrderTotalMaximum).HasPrecision(18, 2);

            builder.HasMany(c => c.RuleSets)
                .WithMany(c => c.CustomerRoles)
                .UsingEntity<Dictionary<string, object>>(
                    "RuleSet_CustomerRole_Mapping",
                    c => c
                        .HasOne<RuleSetEntity>()
                        .WithMany()
                        .HasForeignKey("RuleSetEntity_Id")
                        .HasConstraintName("FK_dbo.RuleSet_CustomerRole_Mapping_dbo.RuleSet_RuleSetEntity_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c => c
                        .HasOne<CustomerRole>()
                        .WithMany()
                        .HasForeignKey("CustomerRole_Id")
                        .HasConstraintName("FK_dbo.RuleSet_CustomerRole_Mapping_dbo.CustomerRole_CustomerRole_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c =>
                    {
                        c.HasIndex("CustomerRole_Id");
                        c.HasKey("CustomerRole_Id", "RuleSetEntity_Id");
                    });
        }
    }

    /// <summary>
    /// Represents a customer role.
    /// </summary>
    [Index(nameof(Active), Name = "IX_Active")]
    [Index(nameof(IsSystemRole), Name = "IX_IsSystemRole")]
    [Index(nameof(SystemName), Name = "IX_SystemName")]
    [Index(nameof(SystemName), nameof(IsSystemRole), Name = "IX_CustomerRole_SystemName_IsSystemRole")]
    public partial class CustomerRole : EntityWithAttributes, IRulesContainer
    {
        public CustomerRole()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private CustomerRole(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the customer role name.
        /// </summary>
        [Required, StringLength(255)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer role is marked as free shiping.
        /// </summary>
        public bool FreeShipping { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer role is marked as tax exempt.
        /// </summary>
        public bool TaxExempt { get; set; }

        /// <summary>
        /// Gets or sets the tax display type.
        /// </summary>
        public int? TaxDisplayType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer role is active.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer role is system.
        /// </summary>
        public bool IsSystemRole { get; set; }

        /// <summary>
        /// Gets or sets the customer role system name.
        /// </summary>
        [StringLength(255)]
        public string SystemName { get; set; }

        /// <summary>
        /// Gets or sets a minimum order amount.
        /// </summary>
        public decimal? OrderTotalMinimum { get; set; }

        /// <summary>
        /// Gets or sets a maximum order amount.
        /// </summary>
        public decimal? OrderTotalMaximum { get; set; }

        private ICollection<PermissionRoleMapping> _permissionRoleMappings;
        /// <summary>
        /// Gets or sets permission role mappings.
        /// </summary>
        [IgnoreDataMember]
        public ICollection<PermissionRoleMapping> PermissionRoleMappings
        {
            get => _permissionRoleMappings ?? LazyLoader.Load(this, ref _permissionRoleMappings) ?? (_permissionRoleMappings ??= new HashSet<PermissionRoleMapping>());
            protected set => _permissionRoleMappings = value;
        }

        private ICollection<RuleSetEntity> _ruleSets;
        /// <summary>
        /// Gets or sets assigned rule sets.
        /// </summary>
        [IgnoreDataMember]
        public ICollection<RuleSetEntity> RuleSets
        {
            get => _ruleSets ?? LazyLoader.Load(this, ref _ruleSets) ?? (_ruleSets ??= new HashSet<RuleSetEntity>());
            protected set => _ruleSets = value;
        }
    }
}
