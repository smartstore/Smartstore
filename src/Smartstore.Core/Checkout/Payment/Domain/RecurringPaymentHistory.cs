using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Smartstore.Core.Checkout.Payment
{
    internal class RecurringPaymentHistoryMap : IEntityTypeConfiguration<RecurringPaymentHistory>
    {
        public void Configure(EntityTypeBuilder<RecurringPaymentHistory> builder)
        {
            builder.HasOne(x => x.RecurringPayment)
                .WithMany(x => x.RecurringPaymentHistory)
                .HasForeignKey(x => x.RecurringPaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Entity framework issue if we have navigation property to 'Order'.
            // 1. save recurring payment with an order
            // 2. save recurring payment history with an order
            // 3. update associated order => exception is thrown
        }
    }

    /// <summary>
    /// Represents a recurring payment history.
    /// </summary>
    public partial class RecurringPaymentHistory : BaseEntity
    {
        public RecurringPaymentHistory()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private RecurringPaymentHistory(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the recurring payment identifier.
        /// </summary>
        public int RecurringPaymentId { get; set; }

        /// <summary>
        /// Gets or sets the recurring payment identifier.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Gets or sets the date and time of entity creation.
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        private RecurringPayment _recurringPayment;
        /// <summary>
        /// Gets the recurring payment.
        /// </summary>
        public RecurringPayment RecurringPayment
        {
            get => _recurringPayment ?? LazyLoader.Load(this, ref _recurringPayment);
            set => _recurringPayment = value;
        }
    }
}