using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Domain;

namespace Smartstore.Core.Checkout.Shipping
{
    public class ShipmentItemMap : IEntityTypeConfiguration<ShipmentItem>
    {
        public void Configure(EntityTypeBuilder<ShipmentItem> builder)
        {
            builder.HasOne(x => x.Shipment)
                .WithMany(x => x.ShipmentItems)
                .HasForeignKey(x => x.ShipmentId)
                .IsRequired(false);
        }
    }

    /// <summary>
    /// Represents a shipment order product variant
    /// </summary>
    [Index(nameof(ShipmentId), Name = "IX_ShipmentId")]
    public partial class ShipmentItem : BaseEntity
    {
        private readonly ILazyLoader _lazyloader;

        public ShipmentItem()
        {
        }

        public ShipmentItem(ILazyLoader lazyLoader)
        {
            _lazyloader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the shipment identifier
        /// </summary>
        public int ShipmentId { get; set; }

        /// <summary>
        /// Gets or sets the order item identifier
        /// </summary>
        public int OrderItemId { get; set; }

        /// <summary>
        /// Gets or sets the quantity
        /// </summary>
        public int Quantity { get; set; }

        private Shipment _shipment;
        /// <summary>
        /// Gets the shipment
        /// </summary>
        public Shipment Shipment
        {
            get => _lazyloader?.Load(this, ref _shipment) ?? _shipment;
            set => _shipment = value;
        }
    }
}