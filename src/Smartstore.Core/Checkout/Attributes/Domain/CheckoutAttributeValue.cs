using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Checkout.Attributes
{
    internal class CheckoutAttributeValueMap : IEntityTypeConfiguration<CheckoutAttributeValue>
    {
        public void Configure(EntityTypeBuilder<CheckoutAttributeValue> builder)
        {
            builder.HasOne(x => x.MediaFile)
                .WithMany()
                .HasForeignKey(x => x.MediaFileId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    /// <summary>
    /// Represents a checkout attribute value
    /// </summary>
    [CacheableEntity]
    [LocalizedEntity("CheckoutAttribute.IsActive")]
    public partial class CheckoutAttributeValue : BaseEntity, ILocalizedEntity
    {
        public CheckoutAttributeValue()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private CheckoutAttributeValue(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the checkout attribute name
        /// </summary>
        [Required]
        [MaxLength(400)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the price adjustment
        /// </summary>
        public decimal PriceAdjustment { get; set; }

        /// <summary>
        /// Gets or sets the weight adjustment
        /// </summary>
        public decimal WeightAdjustment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the value is pre-selected
        /// </summary>
        public bool IsPreSelected { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the color RGB value (used with "Boxes" attribute type).
        /// </summary>
        [StringLength(100)]
        public string Color { get; set; }

        /// <summary>
        /// Gets or sets the checkout attribute mapping identifier
        /// </summary>
        public int CheckoutAttributeId { get; set; }

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
            get => _mediaFile ?? LazyLoader.Load(this, ref _mediaFile);
            set => _mediaFile = value;
        }

        private CheckoutAttribute _checkoutAttribute;
        /// <summary>
        /// Gets or sets the checkout attribute
        /// </summary>
        [IgnoreDataMember]
        public CheckoutAttribute CheckoutAttribute
        {
            get => _checkoutAttribute ?? LazyLoader.Load(this, ref _checkoutAttribute);
            set => _checkoutAttribute = value;
        }
    }
}