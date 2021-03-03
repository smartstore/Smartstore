using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Domain;

namespace Smartstore.Core.Identity
{
    internal class WalletHistoryMap : IEntityTypeConfiguration<WalletHistory>
    {
        public void Configure(EntityTypeBuilder<WalletHistory> builder)
        {
            builder.HasOne(c => c.Customer)
                .WithMany(c => c.WalletHistory)
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            builder.HasOne(c => c.Order)
                .WithMany(c => c.WalletHistory)
                .HasForeignKey(c => c.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    /// <summary>
    /// Represents a digital wallet history entry.
    /// </summary>
    [Index(nameof(StoreId), nameof(CreatedOnUtc), Name = "IX_StoreId_CreatedOn")]
    public partial class WalletHistory : BaseEntity
    {
        private readonly ILazyLoader _lazyLoader;

        public WalletHistory()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private WalletHistory(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
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
            get => _lazyLoader?.Load(this, ref _customer) ?? _customer;
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
            get => _lazyLoader?.Load(this, ref _order) ?? _order;
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
