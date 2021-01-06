using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Localization;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Security;
using Smartstore.Core.Content.Seo;
using Smartstore.Core.Stores;
using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Brands
{
    public class ManufacturerMap : IEntityTypeConfiguration<Manufacturer>
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
    public partial class Manufacturer : EntityWithAttributes, IAuditable, ISoftDeletable, ILocalizedEntity, ISlugSupported, IAclRestricted, IStoreRestricted, IPagingOptions, IDisplayOrder
    {
        private readonly ILazyLoader _lazyLoader;

        public Manufacturer()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private Manufacturer(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the brand name.
        /// </summary>
        [Required, StringLength(400)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [MaxLength]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a description displayed at the bottom of the manufacturer page.
        /// </summary>
        [MaxLength]
        public string BottomDescription { get; set; }

        /// <summary>
        /// Gets or sets the manufacturer template identifier.
        /// </summary>
        public int ManufacturerTemplateId { get; set; }

        /// <summary>
        /// Gets or sets the meta keywords.
        /// </summary>
        [StringLength(400)]
        public string MetaKeywords { get; set; }

        /// <summary>
        /// Gets or sets the meta description.
        /// </summary>
        [StringLength(4000)]
        public string MetaDescription { get; set; }

        /// <summary>
        /// Gets or sets the meta title.
        /// </summary>
        [StringLength(400)]
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
            get => _lazyLoader?.Load(this, ref _mediaFile) ?? _mediaFile;
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
		[JsonIgnore, Obsolete("Price ranges are calculated automatically since version 3.")]
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
        [JsonIgnore]
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <inheritdoc/>
        public DateTime CreatedOnUtc { get; set; }

        /// <inheritdoc/>
        public DateTime UpdatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this manufacturer has discounts applied.
        /// </summary>
        /// <remarks>
        /// We use this property for performance optimization:
        /// if this property is set to false, then we do not need to load AppliedDiscounts navigation property.
        /// </remarks>
        public bool HasDiscountsApplied { get; set; }

        private ICollection<Discount> _appliedDiscounts;
        /// <summary>
        /// Gets or sets the applied discounts.
        /// </summary>
        public ICollection<Discount> AppliedDiscounts
        {
            get => _lazyLoader?.Load(this, ref _appliedDiscounts) ?? (_appliedDiscounts ??= new HashSet<Discount>());
            protected set => _appliedDiscounts = value;
        }

        /// <inheritdoc/>
        public string GetDisplayName()
        {
            return Name;
        }

        /// <inheritdoc/>
        public string GetDisplayNameMemberName()
        {
            return nameof(Name);
        }
    }
}
