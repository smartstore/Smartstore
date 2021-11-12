using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common;
using Smartstore.Domain;

namespace Smartstore.Shipping.Domain
{
    internal class ShippingRateEntityMap : IEntityTypeConfiguration<ShippingRateByTotal>
    {
        public void Configure(EntityTypeBuilder<ShippingRateByTotal> builder)
        {
            builder.HasOne(c => c.ShippingMethod)
                .WithMany()
                .HasForeignKey(c => c.ShippingMethodId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Country)
                .WithMany()
                .HasForeignKey(c => c.CountryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.StateProvince)
                .WithMany()
                .HasForeignKey(c => c.StateProvinceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents a Shipping rate.
    /// </summary>
    [Table("ShippingByTotal")]
    public partial class ShippingRateByTotal : BaseEntity
    {
        public ShippingRateByTotal()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ShippingRateByTotal(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the shipping method identifier.
        /// </summary>
        public int ShippingMethodId { get; set; }

        private ShippingMethod _shippingMethod;
        /// <summary>
        /// Gets or sets the store.
        /// </summary>
        public ShippingMethod ShippingMethod
        {
            get => _shippingMethod ?? LazyLoader.Load(this, ref _shippingMethod);
            set => _shippingMethod = value;
        }

        /// <summary>
        /// Gets or sets the store identifier
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// Gets or sets the country identifier
        /// </summary>
        public int? CountryId { get; set; }

        private Country _country;
        /// <summary>
        /// Gets or sets the country.
        /// </summary>
        public Country Country
        {
            get => _country ?? LazyLoader.Load(this, ref _country);
            set => _country = value;
        }

        /// <summary>
        /// Gets or sets the state/province identifier
        /// </summary>
        public int? StateProvinceId { get; set; }

        private StateProvince _stateProvince;
        /// <summary>
        /// Gets or sets the state province.
        /// </summary>
        public StateProvince StateProvince
        {
            get => _stateProvince ?? LazyLoader.Load(this, ref _stateProvince);
            set => _stateProvince = value;
        }

        /// <summary>
        /// Gets or sets the zip code.
        /// </summary>
        [StringLength(10)]
        public string Zip { get; set; }

        /// <summary>
        /// Gets or sets the "from" value.
        /// </summary>
        public decimal From { get; set; }

        /// <summary>
        /// Gets or sets the "to" value.
        /// </summary>
        public decimal? To { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use percentage.
        /// </summary>
        public bool UsePercentage { get; set; }

        /// <summary>
        /// Gets or sets the shipping charge percentage.
        /// </summary>
        public decimal ShippingChargePercentage { get; set; }

        /// <summary>
        /// Gets or sets the shipping charge amount.
        /// </summary>
        public decimal ShippingChargeAmount { get; set; }

        /// <summary>
        /// Gets or sets the base shipping charge (if <see cref="UsePercentage"/> is set to <c>true</c>).
        /// </summary>
        public decimal BaseCharge { get; set; }

        /// <summary>
        /// Gets or sets the max shipping charge (if <see cref="UsePercentage"/> is set to <c>true</c>).
        /// </summary>
        public decimal? MaxCharge { get; set; }
    }
}
