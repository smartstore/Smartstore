using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Represents a return request.
    /// </summary>
    public partial class ReturnRequest : BaseEntity, IAuditable
    {
        public ReturnRequest()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ReturnRequest(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the store identifier.
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// Gets or sets the order item identifier.
        /// </summary>
        public int OrderItemId { get; set; }

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
        /// Gets or sets the quantity.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the reason for return.
        /// </summary>
        [Required, StringLength(4000)]
        public string ReasonForReturn { get; set; }

        /// <summary>
        /// Gets or sets the requested action.
        /// </summary>
        [Required, StringLength(4000)]
        public string RequestedAction { get; set; }

        /// <summary>
        /// Gets or sets the date and time when requested action was last updated.
        /// </summary>
        public DateTime? RequestedActionUpdatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the customer comments.
        /// </summary>
        public string CustomerComments { get; set; }

        /// <summary>
        /// Gets or sets the staff notes.
        /// </summary>
        public string StaffNotes { get; set; }

        /// <summary>
        /// Gets or sets the admin comment.
        /// </summary>
        [StringLength(4000)]
        public string AdminComment { get; set; }

        /// <summary>
        /// Gets or sets the return status identifier.
        /// </summary>
        public int ReturnRequestStatusId { get; set; }

        /// <summary>
        /// Gets or sets the return status.
        /// </summary>
        [NotMapped]
        public ReturnRequestStatus ReturnRequestStatus
        {
            get => (ReturnRequestStatus)ReturnRequestStatusId;
            set => ReturnRequestStatusId = (int)value;
        }

        /// <summary>
        /// Gets or sets whether to refund to wallet.
        /// </summary>
        public bool? RefundToWallet { get; set; }

        /// <inheritdoc/>
        public DateTime CreatedOnUtc { get; set; }

        /// <inheritdoc/>
        public DateTime UpdatedOnUtc { get; set; }
    }
}
