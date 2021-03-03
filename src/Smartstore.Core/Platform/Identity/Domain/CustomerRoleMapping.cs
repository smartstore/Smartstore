using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Domain;

namespace Smartstore.Core.Identity
{
    internal class CustomerRoleMappingMap : IEntityTypeConfiguration<CustomerRoleMapping>
    {
        public void Configure(EntityTypeBuilder<CustomerRoleMapping> builder)
        {
            builder.HasOne(c => c.Customer)
                .WithMany(c => c.CustomerRoleMappings)
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            builder.HasOne(c => c.CustomerRole)
                .WithMany()
                .HasForeignKey(c => c.CustomerRoleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents a customer to customer role mapping.
    /// </summary>
    [Index(nameof(IsSystemMapping), Name = "IX_IsSystemMapping")]
    public partial class CustomerRoleMapping : BaseEntity
    {
        private readonly ILazyLoader _lazyLoader;

        public CustomerRoleMapping()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private CustomerRoleMapping(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the customer identifier.
        /// </summary>
        public int CustomerId { get; set; }

        private Customer _customer;
        /// <summary>
        /// Gets or sets the customer.
        /// </summary>
        public Customer Customer
        {
            get => _lazyLoader?.Load(this, ref _customer) ?? _customer;
            set => _customer = value;
        }

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
            get => _lazyLoader?.Load(this, ref _customerRole) ?? _customerRole;
            set => _customerRole = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the mapping is created by the user or by the system.
        /// </summary>
        public bool IsSystemMapping { get; set; }
    }
}
