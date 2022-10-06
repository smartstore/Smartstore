using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Catalog.Pricing
{
    internal class TierPriceMap : IEntityTypeConfiguration<TierPrice>
    {
        public void Configure(EntityTypeBuilder<TierPrice> builder)
        {
            builder.HasOne(c => c.Product)
                .WithMany(c => c.TierPrices)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.CustomerRole)
                .WithMany()
                .HasForeignKey(c => c.CustomerRoleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents a tier price.
    /// </summary>
    public partial class TierPrice : BaseEntity
    {
        public TierPrice()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private TierPrice(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the product identifier.
        /// </summary>
        public int ProductId { get; set; }

        private Product _product;
        /// <summary>
        /// Gets or sets the product.
        /// </summary>
        [IgnoreDataMember]
        public Product Product
        {
            get => _product ?? LazyLoader.Load(this, ref _product);
            set => _product = value;
        }

        /// <summary>
        /// Gets or sets the store identifier. 0 means all stores.
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the tier price.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the tier price calculation method.
        /// </summary>
        public TierPriceCalculationMethod CalculationMethod { get; set; }

        /// <summary>
        /// Gets or sets the customer role identifier.
        /// </summary>
        public int? CustomerRoleId { get; set; }

        private CustomerRole _customerRole;
        /// <summary>
        /// Gets or sets the customer role.
        /// </summary>
        [IgnoreDataMember]
        public CustomerRole CustomerRole
        {
            get => _customerRole ?? LazyLoader.Load(this, ref _customerRole);
            set => _customerRole = value;
        }
    }
}
