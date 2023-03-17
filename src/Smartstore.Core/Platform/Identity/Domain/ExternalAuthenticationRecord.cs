using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Smartstore.Core.Identity
{
    /// <summary>
    /// Represents an external authentication record.
    /// </summary>
    [Index(nameof(ProviderSystemName), nameof(ExternalIdentifier))]
    public partial class ExternalAuthenticationRecord : BaseEntity
    {
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
        [StringLength(255)]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the external identifier.
        /// </summary>
        [StringLength(400)]
        public string ExternalIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the external display identifier.
        /// </summary>
        [StringLength(400)]
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
        [StringLength(255)]
        public string ProviderSystemName { get; set; }
    }
}
