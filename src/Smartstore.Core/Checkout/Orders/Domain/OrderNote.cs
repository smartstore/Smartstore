using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Domain;

namespace Smartstore.Core.Checkout.Orders
{
    public class OrderNoteMap : IEntityTypeConfiguration<OrderNote>
    {
        public void Configure(EntityTypeBuilder<OrderNote> builder)
        {
            builder.HasOne(x => x.Order)
                .WithMany(x => x.OrderNotes)
                .HasForeignKey(x => x.OrderId)
                .IsRequired(false);
        }
    }

    /// <summary>
    /// Represents an order note
    /// </summary>
    public partial class OrderNote : BaseEntity
    {
        private readonly ILazyLoader _lazyLoader;

        public OrderNote()
        {
        }

        public OrderNote(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
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
        [JsonIgnore]
        public Order Order 
        {
            get => _lazyLoader?.Load(this, ref _order) ?? _order;
            set => _order = value;
        }
    }
}