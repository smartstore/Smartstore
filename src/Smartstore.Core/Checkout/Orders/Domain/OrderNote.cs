using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Smartstore.Core.Checkout.Orders
{
    internal class OrderNoteMap : IEntityTypeConfiguration<OrderNote>
    {
        public void Configure(EntityTypeBuilder<OrderNote> builder)
        {
            builder.HasOne(x => x.Order)
                .WithMany(x => x.OrderNotes)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents an order note
    /// </summary>
    public partial class OrderNote : BaseEntity
    {
        public OrderNote()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private OrderNote(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the order identifier
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Gets or sets the note
        /// </summary>
        [Required, MaxLength]
        public string Note { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a customer can see a note
        /// </summary>
        public bool DisplayToCustomer { get; set; }

        /// <summary>
        /// Gets or sets the date and time of order note creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        private Order _order;
        /// <summary>
        /// Gets the order
        /// </summary>
        public Order Order
        {
            get => _order ?? LazyLoader.Load(this, ref _order);
            set => _order = value;
        }
    }
}