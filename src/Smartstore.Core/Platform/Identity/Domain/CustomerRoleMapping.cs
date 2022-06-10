using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Smartstore.Core.Identity
{
    /// <summary>
    /// Represents a customer to customer role mapping.
    /// </summary>
    [Index(nameof(IsSystemMapping), Name = "IX_IsSystemMapping")]
    public partial class CustomerRoleMapping : BaseEntity
    {
        public CustomerRoleMapping()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private CustomerRoleMapping(ILazyLoader lazyLoader)
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
        /// Gets or sets a value indicating whether the mapping is created by the user or by the system.
        /// </summary>
        public bool IsSystemMapping { get; set; }
    }
}
