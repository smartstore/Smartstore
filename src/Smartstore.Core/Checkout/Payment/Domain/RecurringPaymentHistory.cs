using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Domain;
using System;

namespace Smartstore.Core.Checkout.Payment.Domain
{
    public class RecurringPaymentHistoryMap : IEntityTypeConfiguration<RecurringPaymentHistory>
    {
        public void Configure(EntityTypeBuilder<RecurringPaymentHistory> builder)
        {
            builder.HasOne(x => x.RecurringPayment)
                .WithMany(x => x.RecurringPaymentHistory)
                .HasForeignKey(x => x.RecurringPaymentId);
        }
    }

    /// <summary>
    /// Represents a recurring payment history
    /// </summary>
    public partial class RecurringPaymentHistory : BaseEntity
    {
        private readonly ILazyLoader _lazyLoader;

        public RecurringPaymentHistory()
        {
        }

        public RecurringPaymentHistory(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the recurring payment identifier
        /// </summary>
        public int RecurringPaymentId { get; set; }

        /// <summary>
        /// Gets or sets the recurring payment identifier
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Gets or sets the date and time of entity creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        private RecurringPayment _recurringPayment;
        /// <summary>
        /// Gets the recurring payment
        /// </summary>
        public RecurringPayment RecurringPayment
        {
            get => _lazyLoader?.Load(this, ref _recurringPayment) ?? _recurringPayment;
            set => _recurringPayment = value;
        }

    }
}
