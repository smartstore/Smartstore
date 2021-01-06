using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Customers;
using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Pricing
{
    public class TierPriceMap : IEntityTypeConfiguration<TierPrice>
    {
        public void Configure(EntityTypeBuilder<TierPrice> builder)
        {
            builder.HasOne(c => c.Product)
                .WithMany(c => c.TierPrices)
                .HasForeignKey(c => c.ProductId)
                .IsRequired(false);

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
        private readonly ILazyLoader _lazyLoader;

        public TierPrice()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private TierPrice(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the product identifier.
        /// </summary>
        public int ProductId { get; set; }

        private Product _product;
        /// <summary>
        /// Gets or sets the product.
        /// </summary>
        [JsonIgnore]
        public Product Product
        {
            get => _lazyLoader?.Load(this, ref _product) ?? _product;
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
        [JsonIgnore]
        public CustomerRole CustomerRole
        {
            get => _lazyLoader?.Load(this, ref _customerRole) ?? _customerRole;
            set => _customerRole = value;
        }
    }
}
