using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Represents a return case.
    /// </summary>
    public partial class ReturnCase : BaseEntity, IAuditable
    {
        public ReturnCaseKind Kind { get; set; }

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
        /// Gets or sets the withdrawal identifier.
        /// </summary>
        /// <remarks>
        /// The withdrawal entity is not part of the core and can be provided by a plugin.
        /// This property is intended for the necessary foreign key relationship.
        /// </remarks>
        public int? WithdrawalId { get; set; }

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the reason for return.
        /// </summary>
        [StringLength(4000)]
        public string ReasonForReturn { get; set; }

        /// <summary>
        /// Gets or sets the requested action.
        /// </summary>
        [StringLength(4000)]
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

        public int ReturnCaseStatusId { get; set; }

        [NotMapped]
        public ReturnCaseStatus ReturnCaseStatus
        {
            get => (ReturnCaseStatus)ReturnCaseStatusId;
            set => ReturnCaseStatusId = (int)value;
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
