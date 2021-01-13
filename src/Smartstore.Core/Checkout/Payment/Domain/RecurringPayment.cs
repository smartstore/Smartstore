using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Domain;

namespace Smartstore.Core.Checkout.Payment
{
    public class RecurringPaymentMap : IEntityTypeConfiguration<RecurringPayment>
    {
        public void Configure(EntityTypeBuilder<RecurringPayment> builder)
        {
            builder.HasOne(rp => rp.InitialOrder)
                .WithMany()
                .HasForeignKey(o => o.InitialOrderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    /// <summary>
    /// Represents a recurring payment
    /// </summary>
    public partial class RecurringPayment : EntityWithAttributes, ISoftDeletable
    {
        private readonly ILazyLoader _lazyLoader;

        public RecurringPayment()
        {
        }

        public RecurringPayment(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the cycle length
        /// </summary>
        public int CycleLength { get; set; }

        /// <summary>
        /// Gets or sets the cycle period identifier
        /// </summary>
        public int CyclePeriodId { get; set; }

        /// <summary>
        /// Gets or sets the payment status
        /// </summary>
        [NotMapped]
        public RecurringProductCyclePeriod CyclePeriod
        {
            get => (RecurringProductCyclePeriod)CyclePeriodId;
            set => CyclePeriodId = (int)value;
        }

        /// <summary>
        /// Gets or sets the total cycles
        /// </summary>
        public int TotalCycles { get; set; }

        /// <summary>
        /// Gets or sets the start date
        /// </summary>
        public DateTime StartDateUtc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the payment is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity has been deleted
        /// </summary>
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets the initial order identifier
        /// </summary>
        public int InitialOrderId { get; set; }

        /// <summary>
        /// Gets or sets the date and time of payment creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets the next payment date
        /// </summary>
        [NotMapped]
        public DateTime? NextPaymentDate
        {
            get
            {

                if (!IsActive || RecurringPaymentHistory.Count >= TotalCycles)
                    return null;

                DateTime? result = null;
                if (RecurringPaymentHistory.Count > 0)
                {
                    result = CyclePeriod switch
                    {
                        RecurringProductCyclePeriod.Days => StartDateUtc.AddDays((double)CycleLength * RecurringPaymentHistory.Count),
                        RecurringProductCyclePeriod.Weeks => StartDateUtc.AddDays((double)(7 * CycleLength) * RecurringPaymentHistory.Count),
                        RecurringProductCyclePeriod.Months => StartDateUtc.AddMonths(CycleLength * RecurringPaymentHistory.Count),
                        RecurringProductCyclePeriod.Years => StartDateUtc.AddYears(CycleLength * RecurringPaymentHistory.Count),
                        _ => throw new SmartException("Not supported cycle period"),
                    };
                }
                else if (TotalCycles > 0)
                {
                    result = StartDateUtc;
                }

                return result;
            }
        }

        /// <summary>
        /// Gets the cycles remaining
        /// </summary>
        [NotMapped]
        public int CyclesRemaining => Math.Clamp(TotalCycles - RecurringPaymentHistory.Count, 0, int.MaxValue);

        private ICollection<RecurringPaymentHistory> _recurringPaymentHistory;
        /// <summary>
        /// Gets or sets the recurring payment history
        /// </summary>
        public ICollection<RecurringPaymentHistory> RecurringPaymentHistory
        {
            get => _lazyLoader?.Load(this, ref _recurringPaymentHistory) ?? (_recurringPaymentHistory ??= new List<RecurringPaymentHistory>());
            protected set => _recurringPaymentHistory = value;
        }

        private Order _initialOrder;
        /// <summary>
        /// Gets the initial order
        /// </summary>
        public Order InitialOrder
        {
            get => _lazyLoader?.Load(this, ref _initialOrder) ?? _initialOrder;
            set => _initialOrder = value;
        }
    }
}