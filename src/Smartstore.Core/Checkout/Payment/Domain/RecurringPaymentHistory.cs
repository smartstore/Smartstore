﻿using Microsoft.EntityFrameworkCore.Infrastructure;
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
        }
    }

    /// <summary>
    /// Represents a recurring payment history.
    /// </summary>
    public partial class RecurringPaymentHistory : BaseEntity
    {
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