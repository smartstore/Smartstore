using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Domain;

namespace Smartstore.Core.Identity
{
    internal class ExternalAuthenticationRecordMap : IEntityTypeConfiguration<ExternalAuthenticationRecord>
    {
        public void Configure(EntityTypeBuilder<ExternalAuthenticationRecord> builder)
        {
            //builder.HasOne(c => c.Customer)
            //    .WithMany(c => c.ExternalAuthenticationRecords)
            //    .HasForeignKey(c => c.CustomerId)
            //    .IsRequired(false)
            //    .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents an external authentication record.
    /// </summary>
    public partial class ExternalAuthenticationRecord : BaseEntity
    {
        public ExternalAuthenticationRecord()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ExternalAuthenticationRecord(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {            
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
            get => _customer ?? LazyLoader.Load(this, ref _customer);
            set => _customer = value;
        }

        /// <summary>
        /// Gets or sets the external email.
        /// </summary>
        [StringLength(4000)]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the external identifier.
        /// </summary>
        [StringLength(4000)]
        public string ExternalIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the external display identifier.
        /// </summary>
        [StringLength(4000)]
        public string ExternalDisplayIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the OAuthToken.
        /// </summary>
        [StringLength(4000)]
        public string OAuthToken { get; set; }

        /// <summary>
        /// Gets or sets the OAuthAccessToken.
        /// </summary>
        [StringLength(4000)]
        public string OAuthAccessToken { get; set; }

        /// <summary>
        /// Gets or sets the provider system name.
        /// </summary>
        [StringLength(4000)]
        public string ProviderSystemName { get; set; }
    }
}
