using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Core.Checkout.Payment
{
    internal class RecurringPaymentMap : IEntityTypeConfiguration<RecurringPayment>
    {
        public void Configure(EntityTypeBuilder<RecurringPayment> builder)
        {
            // Globally exclude soft-deleted entities from all queries.
            builder.HasQueryFilter(c => !c.Deleted);

            builder.HasOne(x => x.InitialOrder)
                .WithMany()
                .HasForeignKey(x => x.InitialOrderId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

    /// <summary>
    /// Represents a recurring payment.
    /// </summary>
    public partial class RecurringPayment : EntityWithAttributes, ISoftDeletable
    {
        public RecurringPayment()
        {
        }

        public RecurringPayment(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the cycle length.
        /// </summary>
        public int CycleLength { get; set; }

        /// <summary>
        /// Gets or sets the cycle period identifier.
        /// </summary>
        public int CyclePeriodId { get; set; }

        /// <summary>
        /// Gets or sets the payment status.
        /// </summary>
        [NotMapped]
        public RecurringProductCyclePeriod CyclePeriod
        {
            get => (RecurringProductCyclePeriod)CyclePeriodId;
            set => CyclePeriodId = (int)value;
        }

        /// <summary>
        /// Gets or sets the total cycles.
        /// </summary>
        public int TotalCycles { get; set; }

        /// <summary>
        /// Gets or sets the start date.
        /// </summary>
        public DateTime StartDateUtc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the payment is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity has been deleted.
        /// </summary>
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets the initial order identifier.
        /// </summary>
        public int InitialOrderId { get; set; }

        /// <summary>
        /// Gets or sets the date and time of payment creation.
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        private ICollection<RecurringPaymentHistory> _recurringPaymentHistory;
        /// <summary>
        /// Gets or sets the recurring payment history.
        /// </summary>
        public ICollection<RecurringPaymentHistory> RecurringPaymentHistory
        {
            get => _recurringPaymentHistory = LazyLoader.Load(this, ref _recurringPaymentHistory) ?? (_recurringPaymentHistory ??= new List<RecurringPaymentHistory>());
            protected set => _recurringPaymentHistory = value;
        }

        private Order _initialOrder;
        /// <summary>
        /// Gets the initial order.
        /// </summary>
        public Order InitialOrder
        {
            get => _initialOrder ?? LazyLoader.Load(this, ref _initialOrder);
            set => _initialOrder = value;
        }
    }
}