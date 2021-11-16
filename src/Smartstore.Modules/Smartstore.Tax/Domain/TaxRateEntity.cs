using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Smartstore.Tax.Domain
{
    internal class TaxRateEntityMap : IEntityTypeConfiguration<TaxRateEntity>
    {
        public void Configure(EntityTypeBuilder<TaxRateEntity> builder)
        {
            builder.HasOne(c => c.TaxCategory)
                .WithMany()
                .HasForeignKey(c => c.TaxCategoryId)
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
    /// Represents a tax rate.
    /// </summary>
    [Table("TaxRate")]
    public partial class TaxRateEntity : BaseEntity
    {
        public TaxRateEntity()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private TaxRateEntity(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the tax category identifier
        /// </summary>
        public int TaxCategoryId { get; set; }

        private TaxCategory _taxCategory;
        /// <summary>
        /// Gets or sets the tax category.
        /// </summary>
        public TaxCategory TaxCategory
        {
            get => _taxCategory ?? LazyLoader.Load(this, ref _taxCategory);
            set => _taxCategory = value;
        }

        /// <summary>
        /// Gets or sets the country identifier
        /// </summary>
        public int CountryId { get; set; }

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
        public int StateProvinceId { get; set; }

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
        [StringLength(100)]
        public string Zip { get; set; }

        /// <summary>
        /// Gets or sets the percentage.
        /// </summary>
        public decimal Percentage { get; set; }
    }
}
