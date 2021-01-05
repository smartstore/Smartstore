using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Domain;

namespace Smartstore.Core.Checkout.Shipping
{
    public class ShipmentMap : IEntityTypeConfiguration<Shipment>
    {
        public void Configure(EntityTypeBuilder<Shipment> builder)
        {
            builder.HasOne(x => x.Order)
                .WithMany(x => x.Shipments)
                .HasForeignKey(x => x.OrderId)
                .IsRequired(false);
        }
    }

    /// <summary>
    /// Represents a shipment
    /// </summary>
    public partial class Shipment : EntityWithAttributes
    {
        private readonly ILazyLoader _lazyLoader;

        public Shipment()
        {
        }

        public Shipment(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the order identifier
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Gets or sets the tracking number of this shipment
        /// </summary>
        public string TrackingNumber { get; set; }

        /// <summary>
        /// Gets or sets the tracking URL.
        /// </summary>
        [StringLength(2000)]
        public string TrackingUrl { get; set; }

        /// <summary>
        /// Gets or sets the total weight of this shipment
        /// It's nullable for compatibility with the previous version of Smartstore where was no such property
        /// </summary>
        public decimal? TotalWeight { get; set; }

        /// <summary>
        /// Gets or sets the shipped date and time
        /// </summary>
        public DateTime? ShippedDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the delivery date and time
        /// </summary>
        public DateTime? DeliveryDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the entity creation date
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        private Order _order;
        /// <summary>
        /// Gets or sets the order
        /// </summary>
        public Order Order 
        {
            get => _lazyLoader?.Load(this, ref _order) ?? _order;
            set => _order = value;
        }

        private ICollection<ShipmentItem> _shipmentItems;
        /// <summary>
		/// Gets or sets the shipment items
        /// </summary>
        public ICollection<ShipmentItem> ShipmentItems
        {
            get => _lazyLoader?.Load(this, ref _shipmentItems) ?? (_shipmentItems ??= new HashSet<ShipmentItem>());
            protected set => _shipmentItems = value;
        }
    }
}