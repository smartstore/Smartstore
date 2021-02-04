using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Domain;

namespace Smartstore.Core.Identity
{
    public class CustomerContentMap : IEntityTypeConfiguration<CustomerContent>
    {
        public void Configure(EntityTypeBuilder<CustomerContent> builder)
        {
            builder.HasOne(c => c.Customer)
                .WithMany(c => c.CustomerContent)
                .HasForeignKey(c => c.CustomerId)
                .IsRequired(false);
        }
    }

    /// <summary>
    /// Represents content entered by a customer.
    /// </summary>
    public partial class CustomerContent : BaseEntity, IAuditable
    {
        private readonly ILazyLoader _lazyLoader;

        public CustomerContent()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private CustomerContent(ILazyLoader lazyLoader)
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
        /// Gets or sets the IP address.
        /// </summary>
        [StringLength(200)]
        public string IpAddress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the content is approved.
        /// </summary>
        public bool IsApproved { get; set; }

        /// <inheritdoc/>
        public DateTime CreatedOnUtc { get; set; }

        /// <inheritdoc/>
        public DateTime UpdatedOnUtc { get; set; }
    }
}
