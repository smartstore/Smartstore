using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Brands
{
    internal class ManufacturerMap : IEntityTypeConfiguration<Manufacturer>
    {
        public void Configure(EntityTypeBuilder<Manufacturer> builder)
        {
            builder.HasQueryFilter(c => !c.Deleted);

            builder.HasOne(c => c.MediaFile)
                .WithMany()
                .HasForeignKey(c => c.MediaFileId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    /// <summary>
    /// Represents a manufacturer.
    /// </summary>
    [Index(nameof(Deleted), Name = "IX_Deleted")]
    [Index(nameof(DisplayOrder), Name = "IX_Manufacturer_DisplayOrder")]
    [Index(nameof(LimitedToStores), Name = "IX_Manufacturer_LimitedToStores")]
    [Index(nameof(SubjectToAcl), Name = "IX_SubjectToAcl")]
    [LocalizedEntity("Published and !Deleted")]
    public partial class Manufacturer : EntityWithDiscounts, IAuditable, ISoftDeletable, ILocalizedEntity, ISlugSupported, IAclRestricted, IStoreRestricted, IPagingOptions, IDisplayOrder
    {
        public Manufacturer()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private Manufacturer(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the brand name.
        /// </summary>
        [Required, StringLength(400)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [MaxLength]
        [LocalizedProperty]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a description displayed at the bottom of the manufacturer page.
        /// </summary>
        [MaxLength]
        [LocalizedProperty]
        public string BottomDescription { get; set; }

        /// <summary>
        /// Gets or sets the manufacturer template identifier.
        /// </summary>
        public int ManufacturerTemplateId { get; set; }

        /// <summary>
        /// Gets or sets the meta keywords.
        /// </summary>
        [StringLength(400)]
        [LocalizedProperty]
        public string MetaKeywords { get; set; }

        /// <summary>
        /// Gets or sets the meta description.
        /// </summary>
        [StringLength(4000)]
        [LocalizedProperty]
        public string MetaDescription { get; set; }

        /// <summary>
        /// Gets or sets the meta title.
        /// </summary>
        [StringLength(400)]
        [LocalizedProperty]
        public string MetaTitle { get; set; }

        /// <summary>
        /// Gets or sets the media file identifier.
        /// </summary>
        public int? MediaFileId { get; set; }

        private MediaFile _mediaFile;
        /// <summary>
        /// Gets or sets the media file.
        /// </summary>
        public MediaFile MediaFile
        {
            get => _mediaFile ?? LazyLoader?.Load(this, ref _mediaFile);
            set => _mediaFile = value;
        }

        /// <inheritdoc/>
        public int? PageSize { get; set; }

        /// <inheritdoc/>
        public bool? AllowCustomersToSelectPageSize { get; set; }

        /// <inheritdoc/>
        [StringLength(200)]
        public string PageSizeOptions { get; set; }

        /// <summary>
        /// Gets or sets the available price ranges.
        /// </summary>
		[IgnoreDataMember, Obsolete("Price ranges are calculated automatically since version 3.")]
        [StringLength(400)]
        public string PriceRanges { get; set; }

        /// <inheritdoc/>
        public bool LimitedToStores { get; set; }

        /// <inheritdoc/>
        public bool SubjectToAcl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is published.
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity has been deleted.
        /// </summary>
        [IgnoreDataMember]
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <inheritdoc/>
        public DateTime CreatedOnUtc { get; set; }

        /// <inheritdoc/>
        public DateTime UpdatedOnUtc { get; set; }

        /// <inheritdoc/>
        public string GetDisplayName()
        {
            return Name;
        }

        /// <inheritdoc/>
        public string[] GetDisplayNameMemberNames()
        {
            return new[] { nameof(Name) };
        }
    }
}
