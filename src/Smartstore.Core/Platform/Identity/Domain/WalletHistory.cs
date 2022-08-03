using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Core.Identity
{
    /// <summary>
    /// Represents a digital wallet history entry.
    /// </summary>
    [Index(nameof(StoreId), nameof(CreatedOnUtc), Name = "IX_StoreId_CreatedOn")]
    public partial class WalletHistory : BaseEntity
    {
        public WalletHistory()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private WalletHistory(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the store identifier. Should not be zero.
        /// </summary>
        public int StoreId { get; set; }

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
        /// Gets or sets the order identifier.
        /// </summary>
        public int? OrderId { get; set; }

        private Order _order;
        /// <summary>
        /// Gets or sets the order.
        /// </summary>
        public Order Order
        {
            get => _order ?? LazyLoader.Load(this, ref _order);
            set => _order = value;
        }

        /// <summary>
        /// Gets or sets the amount of the entry.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Gets or sets the amount balance when the entry was created.
        /// </summary>
        public decimal AmountBalance { get; set; }

        /// <summary>
        /// Gets or sets the amount balance per store when the entry was created.
        /// </summary>
        public decimal AmountBalancePerStore { get; set; }

        /// <summary>
        /// Gets or sets the date when the entry was created (in UTC).
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the reason for posting this entry.
        /// </summary>
        public WalletPostingReason? Reason { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        [StringLength(1000)]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the admin comment.
        /// </summary>
        [StringLength(4000)]
        public string AdminComment { get; set; }
    }
}
