using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Common
{
    internal class CountryMap : IEntityTypeConfiguration<Country>
    {
        public void Configure(EntityTypeBuilder<Country> builder)
        {
            builder
                .HasOne(x => x.DefaultCurrency)
                .WithMany()
                .HasForeignKey(x => x.DefaultCurrencyId)
                .OnDelete(DeleteBehavior.SetNull);

            builder
                .HasMany(x => x.StateProvinces)
                .WithOne(x => x.Country)
                .HasForeignKey(x => x.CountryId);
        }
    }

    /// <summary>
    /// Represents a country
    /// </summary>
    [Index(nameof(DisplayOrder), Name = "IX_Country_DisplayOrder")]
    [CacheableEntity]
    [LocalizedEntity("Published")]
    public partial class Country : EntityWithAttributes, ILocalizedEntity, IStoreRestricted, IDisplayOrder
    {
        public Country()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private Country(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [MaxLength(4000)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether billing is allowed to this country
        /// </summary>
        public bool AllowsBilling { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether shipping is allowed to this country
        /// </summary>
        public bool AllowsShipping { get; set; }

        /// <summary>
        /// Gets or sets the two letter ISO code
        /// </summary>
        public string TwoLetterIsoCode { get; set; }

        /// <summary>
        /// Gets or sets the three letter ISO code
        /// </summary>
        public string ThreeLetterIsoCode { get; set; }

        /// <summary>
        /// Gets or sets the numeric ISO code
        /// </summary>
        public int NumericIsoCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether customers in this country must be charged EU VAT
        /// </summary>
        public bool SubjectToVat { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is published
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the cookie manager should be displayed for visitors of this country.
        /// </summary>
        public bool DisplayCookieManager { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores
        /// </summary>
        public bool LimitedToStores { get; set; }

        /// <summary>
        /// Gets or sets the international mailing address format
        /// </summary>
        [MaxLength]
        public string AddressFormat { get; set; }

        /// <summary>
        /// Gets or sets the international dailling code
        /// </summary>
        [MaxLength]
        public int? DiallingCode { get; set; }
        
        /// <summary>
        /// Gets or sets the identfier of the default currency.
        /// </summary>
        public int? DefaultCurrencyId { get; set; }

        private Currency _defaultCurrency;
        /// <summary>
        /// Gets or sets the default currency.
        /// </summary>
        [IgnoreDataMember]
        public Currency DefaultCurrency
        {
            get => _defaultCurrency ?? LazyLoader.Load(this, ref _defaultCurrency);
            set => _defaultCurrency = value;
        }

        private ICollection<StateProvince> _stateProvinces;
        /// <summary>
        /// Gets or sets the state/provinces
        /// </summary>
        public ICollection<StateProvince> StateProvinces
        {
            get => _stateProvinces ?? LazyLoader.Load(this, ref _stateProvinces) ?? (_stateProvinces ??= new HashSet<StateProvince>());
            protected set => _stateProvinces = value;
        }
    }
}