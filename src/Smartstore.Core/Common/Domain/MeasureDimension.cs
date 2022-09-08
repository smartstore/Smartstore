using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Localization;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Common
{
    internal class MeasureDimensionMap : IEntityTypeConfiguration<MeasureDimension>
    {
        public void Configure(EntityTypeBuilder<MeasureDimension> builder)
        {
            builder.Property(c => c.Ratio).HasPrecision(18, 8);
        }
    }

    /// <summary>
    /// Represents a measure dimension
    /// </summary>
    [CacheableEntity]
    public partial class MeasureDimension : EntityWithAttributes, ILocalizedEntity, IDisplayOrder
    {
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [Required, StringLength(100)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the system keyword
        /// </summary>
        [Required, StringLength(100)]
        public string SystemKeyword { get; set; }

        /// <summary>
        /// Gets or sets the ratio
        /// </summary>
        public decimal Ratio { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}