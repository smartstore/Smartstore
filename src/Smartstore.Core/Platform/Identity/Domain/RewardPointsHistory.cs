using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Core.Identity
{
    /// <summary>
    /// Represents a reward points history entry.
    /// </summary>
    public class RewardPointsHistory : BaseEntity
    {
        public RewardPointsHistory()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private RewardPointsHistory(ILazyLoader lazyLoader)
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
        /// Gets or sets the redeemed/added points.
        /// </summary>
        public int Points { get; set; }

        /// <summary>
        /// Gets or sets the points balance.
        /// </summary>
        public int PointsBalance { get; set; }

        /// <summary>
        /// Gets or sets the used amount.
        /// </summary>
        public decimal UsedAmount { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        [StringLength(4000)]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the date of instance creation.
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        [Column("UsedWithOrder_Id")]
        public int? UsedWithOrderId { get; set; }

        /// <summary>
        /// Gets or sets the order for which points were redeemed as a payment.
        /// </summary>
        public Order UsedWithOrder { get; set; }
    }
}
