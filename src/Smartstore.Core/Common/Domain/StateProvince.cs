using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Localization;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Common
{
    internal class StateProvinceMap : IEntityTypeConfiguration<StateProvince>
    {
        public void Configure(EntityTypeBuilder<StateProvince> builder)
        {
            builder
                .HasOne(x => x.Country)
                .WithMany(x => x.StateProvinces)
                .HasForeignKey(x => x.CountryId);
        }
    }

    /// <summary>
    /// Represents a state/province
    /// </summary>
    [CacheableEntity]
    [LocalizedEntity("Published and Country.Published")]
    public partial class StateProvince : BaseEntity, ILocalizedEntity, IDisplayOrder
    {
        public StateProvince()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private StateProvince(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the country identifier
        /// </summary>
        public int CountryId { get; set; }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [Required, StringLength(100)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the abbreviation
        /// </summary>
        [StringLength(100)]
        public string Abbreviation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is published
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        private Country _country;
        /// <summary>
        /// Gets or sets the country
        /// </summary>
        [IgnoreDataMember]
        public Country Country
        {
            get => _country ?? LazyLoader.Load(this, ref _country);
            set => _country = value;
        }
    }
}