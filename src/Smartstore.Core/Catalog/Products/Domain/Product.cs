using System.Collections.Frozen;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Common;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;

namespace Smartstore.Core.Catalog.Products
{
    internal class ProductMap : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            // Globally exclude soft-deleted entities from all queries.
            builder.HasQueryFilter(c => !c.Deleted);

            builder.HasOne(c => c.DeliveryTime)
                .WithMany()
                .HasForeignKey(c => c.DeliveryTimeId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(c => c.QuantityUnit)
                .WithMany()
                .HasForeignKey(c => c.QuantityUnitId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(c => c.SampleDownload)
                .WithMany()
                .HasForeignKey(c => c.SampleDownloadId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(c => c.CountryOfOrigin)
                .WithMany()
                .HasForeignKey(c => c.CountryOfOriginId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(c => c.ComparePriceLabel)
                .WithMany()
                .HasForeignKey(c => c.ComparePriceLabelId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(c => c.GroupedProductConfiguration)
                .HasConversion(new GroupedProductConfigurationConverter());

            builder
                .HasMany(c => c.ProductTags)
                .WithMany(c => c.Products)
                .UsingEntity<Dictionary<string, object>>(
                    "Product_ProductTag_Mapping",
                    c => c
                        .HasOne<ProductTag>()
                        .WithMany()
                        .HasForeignKey("ProductTag_Id")
                        .HasConstraintName("FK_dbo.Product_ProductTag_Mapping_dbo.ProductTag_ProductTag_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c => c
                        .HasOne<Product>()
                        .WithMany()
                        .HasForeignKey("Product_Id")
                        .HasConstraintName("FK_dbo.Product_ProductTag_Mapping_dbo.Product_Product_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c =>
                    {
                        c.HasIndex("Product_Id");
                        c.HasKey("Product_Id", "ProductTag_Id");
                    });
        }
    }

    /// <summary>
    /// Represents a product.
    /// </summary>
    [Index(nameof(Deleted), Name = "IX_Deleted")]
    [Index(nameof(Gtin), Name = "IX_Gtin")]
    [Index(nameof(IsSystemProduct), Name = "IX_IsSystemProduct")]
    [Index(nameof(ManufacturerPartNumber), Name = "IX_ManufacturerPartNumber")]
    [Index(nameof(LimitedToStores), Name = "IX_Product_LimitedToStores")]
    [Index(nameof(Name), Name = "IX_Product_Name")]
    [Index(nameof(ParentGroupedProductId), Name = "IX_Product_ParentGroupedProductId")]
    [Index(nameof(Price), nameof(AvailableStartDateTimeUtc), nameof(AvailableEndDateTimeUtc), nameof(Published), nameof(Deleted), Name = "IX_Product_PriceDatesEtc")]
    [Index(nameof(Published), Name = "IX_Product_Published")]
    [Index(nameof(Published), nameof(Deleted), nameof(IsSystemProduct), Name = "IX_Product_Published_Deleted_IsSystemProduct")]
    [Index(nameof(ShowOnHomePage), Name = "IX_Product_ShowOnHomepage")]
    [Index(nameof(Sku), Name = "IX_Product_Sku")]
    [Index(nameof(SubjectToAcl), Name = "IX_Product_SubjectToAcl")]
    [Index(nameof(SystemName), nameof(IsSystemProduct), Name = "IX_Product_SystemName_IsSystemProduct")]
    [Index(nameof(Published), nameof(Id), nameof(Visibility), nameof(Deleted), nameof(IsSystemProduct), nameof(AvailableStartDateTimeUtc), nameof(AvailableEndDateTimeUtc), Name = "IX_SeekExport1")]
    [Index(nameof(Visibility), Name = "IX_Visibility")]
    [LocalizedEntity("Published and !Deleted")]
    [DebuggerDisplay("{Id} - {Name}")]
    public partial class Product : EntityWithDiscounts, IAuditable, ISoftDeletable, ILocalizedEntity, ISlugSupported, IAclRestricted, IStoreRestricted, IMergedData
    {
        #region Static

        private static readonly FrozenSet<string> _visibilityAffectingProductProps = new string[]
        {
            nameof(AvailableEndDateTimeUtc),
            nameof(AvailableStartDateTimeUtc),
            nameof(Deleted),
            nameof(LowStockActivityId),
            nameof(LimitedToStores),
            nameof(ManageInventoryMethodId),
            nameof(MinStockQuantity),
            nameof(Published),
            nameof(SubjectToAcl),
            nameof(Visibility)
        }.ToFrozenSet();

        public static IReadOnlyCollection<string> GetVisibilityAffectingPropertyNames()
        {
            return _visibilityAffectingProductProps;
        }

        #endregion

        [NotMapped, IgnoreDataMember]
        public bool MergedDataIgnore { get; set; }

        [NotMapped, IgnoreDataMember]
        public Dictionary<string, object> MergedDataValues { get; set; }

        /// <summary>
        /// Gets or sets the product type identifier.
        /// </summary>
        public int ProductTypeId { get; set; }

        /// <summary>
        /// Gets or sets the product type.
        /// </summary>
        [NotMapped]
        public ProductType ProductType
        {
            get => (ProductType)ProductTypeId;
            set => ProductTypeId = (int)value;
        }

        /// <summary>
        /// Gets the label hint for the product type.
        /// </summary>
        [NotMapped]
        public string ProductTypeLabelHint
        {
            get
            {
                return ProductType switch
                {
                    ProductType.SimpleProduct => "secondary d-none",
                    ProductType.GroupedProduct => "success",
                    ProductType.BundledProduct => "info",
                    _ => "",
                };
            }
        }

        /// <summary>
        /// Gets or sets the parent product identifier. It is used if this product is associated with a grouped product.
        /// </summary>
        public int ParentGroupedProductId { get; set; }

        /// <summary>
        /// Gets or sets the configuration for a grouped product (optional).
        /// </summary>
        [MaxLength, Column("ProductTypeConfiguration")]
        public GroupedProductConfiguration GroupedProductConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the visibility level of the product.
        /// </summary>
        public ProductVisibility Visibility { get; set; }

        [IgnoreDataMember, Obsolete("Use property Visibility instead.")]
        public bool VisibleIndividually { get; set; }

        /// <summary>
        /// Gets or sets the condition of the product.
        /// </summary>
        public ProductCondition Condition { get; set; }

        /// <summary>
        /// Gets or sets the product name.
        /// </summary>
        [Required, StringLength(400)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the short description.
        /// </summary>
        [StringLength(4000)]
        [LocalizedProperty]
        public string ShortDescription { get; set; }

        /// <summary>
        /// Gets or sets the full description.
        /// </summary>
        [MaxLength]
        [LocalizedProperty]
        [NonSummary]
        public string FullDescription { get; set; }

        /// <summary>
        /// Gets or sets the admin comment.
        /// </summary>
        [NonSummary]
        public string AdminComment { get; set; }

        /// <summary>
        /// Gets or sets the product template identifier.
        /// </summary>
        public int ProductTemplateId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show the product on homepage.
        /// </summary>
        public bool ShowOnHomePage { get; set; }

        /// <summary>
        /// Gets or sets the display order for homepage products.
        /// </summary>
        public int HomePageDisplayOrder { get; set; }

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
        /// Gets or sets a value indicating whether customer reviews are allowed for this product.
        /// </summary>
        public bool AllowCustomerReviews { get; set; }

        /// <summary>
        /// Gets or sets the rating sum (approved reviews).
        /// </summary>
        public int ApprovedRatingSum { get; set; }

        /// <summary>
        /// Gets or sets the rating sum (not approved reviews).
        /// </summary>
        public int NotApprovedRatingSum { get; set; }

        /// <summary>
        /// Gets or sets the total rating votes (approved reviews).
        /// </summary>
        public int ApprovedTotalReviews { get; set; }

        /// <summary>
        /// Gets or sets the total rating votes (not approved reviews).
        /// </summary>
        public int NotApprovedTotalReviews { get; set; }

        /// <inheritdoc/>
        public bool SubjectToAcl { get; set; }

        /// <inheritdoc/>
        public bool LimitedToStores { get; set; }

        private string _sku;
        /// <summary>
        /// Gets or sets the stock keeping unit (SKU).
        /// </summary>
        [StringLength(400)]
        public string Sku
        {
            [DebuggerStepThrough]
            get => this.GetMergedDataValue(nameof(Sku), _sku);
            set => _sku = value;
        }

        private string _manufacturerPartNumber;
        /// <summary>
        /// Gets or sets the manufacturer part number (MPN).
        /// </summary>
        [StringLength(400)]
        public string ManufacturerPartNumber
        {
            [DebuggerStepThrough]
            get => this.GetMergedDataValue(nameof(ManufacturerPartNumber), _manufacturerPartNumber);
            set => _manufacturerPartNumber = value;
        }

        private string _gtin;
        /// <summary>
        /// Gets or sets the global trade item number (GTIN).
        /// These identifiers include UPC (in North America), EAN (in Europe), JAN (in Japan) and ISBN (for books).
        /// </summary>
        [StringLength(400)]
        public string Gtin
        {
            [DebuggerStepThrough]
            get => this.GetMergedDataValue(nameof(Gtin), _gtin);
            set => _gtin = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the product is a gift card.
        /// </summary>
        public bool IsGiftCard { get; set; }

        /// <summary>
        /// Gets or sets the gift card type identifier.
        /// </summary>
        public int GiftCardTypeId { get; set; }

        /// <summary>
        /// Gets or sets the gift card type.
        /// </summary>
        [NotMapped]
        public GiftCardType GiftCardType
        {
            get => (GiftCardType)GiftCardTypeId;
            set => GiftCardTypeId = (int)value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the product requires other products to be added to the cart (product X requires product Y).
        /// </summary>
        public bool RequireOtherProducts { get; set; }

        /// <summary>
        /// Gets or sets the required product identifiers (comma separated).
        /// </summary>
        [StringLength(1000)]
        public string RequiredProductIds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether required products are automatically added to the cart.
        /// </summary>
        public bool AutomaticallyAddRequiredProducts { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the product is a download.
        /// </summary>
        public bool IsDownload { get; set; }

        /// <summary>
        /// Gets or sets the download identifier.
        /// </summary>
        [IgnoreDataMember, Obsolete("Since version 3.2 more than one download can be assigned to a product. See property Download.EntityId and Download.EntityName.")]
        public int DownloadId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this product can be downloaded unlimited number of times.
        /// </summary>
        public bool UnlimitedDownloads { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of downloads.
        /// </summary>
        public int MaxNumberOfDownloads { get; set; }

        /// <summary>
        /// Gets or sets the number of days during customers has access to the downloadable file.
        /// </summary>
        public int? DownloadExpirationDays { get; set; }

        /// <summary>
        /// Gets or sets the download activation type.
        /// </summary>
        public int DownloadActivationTypeId { get; set; }

        /// <summary>
        /// Gets or sets the download activation type.
        /// </summary>
        [NotMapped]
        public DownloadActivationType DownloadActivationType
        {
            get => (DownloadActivationType)DownloadActivationTypeId;
            set => DownloadActivationTypeId = (int)value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the product has a sample download file.
        /// </summary>
        public bool HasSampleDownload { get; set; }

        /// <summary>
        /// Gets or sets the sample download identifier.
        /// </summary>
        public int? SampleDownloadId { get; set; }

        private Download _sampleDownload;
        /// <summary>
        /// Gets or sets the sample download.
        /// </summary>
        public Download SampleDownload
        {
            get => _sampleDownload ?? LazyLoader?.Load(this, ref _sampleDownload);
            set => _sampleDownload = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the product has a user agreement.
        /// </summary>
        public bool HasUserAgreement { get; set; }

        /// <summary>
        /// Gets or sets the license agreement text.
        /// </summary>
        [NonSummary]
        public string UserAgreementText { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a recurring product.
        /// </summary>
        public bool IsRecurring { get; set; }

        /// <summary>
        /// Gets or sets the recurring cycle length.
        /// </summary>
        public int RecurringCycleLength { get; set; }

        /// <summary>
        /// Gets or sets the recurring cycle period.
        /// </summary>
        public int RecurringCyclePeriodId { get; set; }

        /// <summary>
        /// Gets or sets the cycle period for recurring products.
        /// </summary>
        [NotMapped]
        public RecurringProductCyclePeriod RecurringCyclePeriod
        {
            get => (RecurringProductCyclePeriod)RecurringCyclePeriodId;
            set => RecurringCyclePeriodId = (int)value;
        }

        /// <summary>
        /// Gets or sets the recurring total cycles.
        /// </summary>
        public int RecurringTotalCycles { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the product is ship enabled.
        /// </summary>
        [Column("IsShipEnabled")]
        public bool IsShippingEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the product has free shipping.
        /// </summary>
        public bool IsFreeShipping { get; set; }

        /// <summary>
        /// Gets or sets the additional shipping charge.
        /// </summary>
        public decimal AdditionalShippingCharge { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the product is marked as tax exempt.
        /// </summary>
        public bool IsTaxExempt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the product is an electronic service
        /// bound to EU VAT regulations for digital goods.
        /// </summary>
        public bool IsEsd { get; set; }

        /// <summary>
        /// Gets or sets the tax category identifier.
        /// </summary>
        public int TaxCategoryId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating how to manage the inventory.
        /// </summary>
        public int ManageInventoryMethodId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating how to manage the inventory.
        /// </summary>
        [NotMapped]
        public ManageInventoryMethod ManageInventoryMethod
        {
            get => (ManageInventoryMethod)ManageInventoryMethodId;
            set => ManageInventoryMethodId = (int)value;
        }

        private int _stockQuantity;
        /// <summary>
        /// Gets or sets the stock quantity.
        /// </summary>
        public int StockQuantity
        {
            [DebuggerStepThrough]
            get => this.GetMergedDataValue(nameof(StockQuantity), _stockQuantity);
            set => _stockQuantity = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to display the stock availability.
        /// </summary>
        public bool DisplayStockAvailability { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display the stock quantity.
        /// </summary>
        public bool DisplayStockQuantity { get; set; }

        /// <summary>
        /// Gets or sets the minimum stock quantity.
        /// </summary>
        public int MinStockQuantity { get; set; }

        /// <summary>
        /// Gets or sets the low stock activity identifier.
        /// </summary>
        public int LowStockActivityId { get; set; }

        /// <summary>
        /// Gets or sets the low stock activity.
        /// </summary>
        [NotMapped]
        public LowStockActivity LowStockActivity
        {
            get => (LowStockActivity)LowStockActivityId;
            set => LowStockActivityId = (int)value;
        }

        /// <summary>
        /// Gets or sets the stock quantity when to notify the admin.
        /// </summary>
        public int NotifyAdminForQuantityBelow { get; set; }

        private int _backorderModeId;
        /// <summary>
        /// Gets or sets the backorder mode identifier.
        /// </summary>
        public int BackorderModeId
        {
            [DebuggerStepThrough]
            get => this.GetMergedDataValue(nameof(BackorderModeId), _backorderModeId);
            set => _backorderModeId = value;
        }

        /// <summary>
        /// Gets or sets the backorder mode.
        /// </summary>
        [NotMapped]
        public BackorderMode BackorderMode
        {
            get => (BackorderMode)BackorderModeId;
            set => BackorderModeId = (int)value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to allow back in stock subscriptions.
        /// </summary>
        public bool AllowBackInStockSubscriptions { get; set; }

        /// <summary>
        /// Gets or sets the minimum order quantity.
        /// </summary>
        public int OrderMinimumQuantity { get; set; }

        /// <summary>
        /// Gets or sets the maximum order quantity.
        /// </summary>
        public int OrderMaximumQuantity { get; set; }

        /// <summary>
        /// Gets or sets a value by which the product quantity can be changed.
        /// </summary>
        public int QuantityStep { get; set; }

        /// <summary>
        /// Gets or sets the quantity control type.
        /// </summary>
        [Column("QuantiyControlType")]
        public QuantityControlType QuantityControlType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to hide the quantity input control.
        /// </summary>
        public bool HideQuantityControl { get; set; }

        /// <summary>
        /// Gets or sets the comma seperated list of allowed quantities. If <c>null</c> or empty then any quantity is allowed.
        /// </summary>
        [StringLength(1000)]
        public string AllowedQuantities { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to disable the "Add to cart" button.
        /// </summary>
        public bool DisableBuyButton { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to disable "Add to wishlist" button.
        /// </summary>
        public bool DisableWishlistButton { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the product is available for pre-ordering.
        /// </summary>
        public bool AvailableForPreOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show "Call for price" label instead of the product price.
        /// </summary>
        public bool CallForPrice { get; set; }

        private decimal _price;
        /// <summary>
        /// Gets or sets the product regular price.
        /// </summary>
        public decimal Price
        {
            [DebuggerStepThrough]
            get => this.GetMergedDataValue(nameof(Price), _price);
            set => _price = value;
        }

        /// <summary>
        /// Gets or sets the compare price, e.g. the retail price.
        /// </summary>
        [Column("OldPrice")]
        public decimal ComparePrice { get; set; }

        /// <summary>
        /// Gets or sets the label id for <see cref="ComparePrice"/>. 
        /// A value set here overwrites the system default <see cref="PriceSettings.DefaultComparePriceLabelId"/>.
        /// </summary>
        public int? ComparePriceLabelId { get; set; }

        private PriceLabel _comparePriceLabel;
        /// <summary>
        /// Gets or sets the compare price label.
        /// </summary>
        public PriceLabel ComparePriceLabel
        {
            get => _comparePriceLabel ?? LazyLoader?.Load(this, ref _comparePriceLabel);
            set => _comparePriceLabel = value;
        }

        /// <summary>
        /// Gets or sets the product cost.
        /// </summary>
        public decimal ProductCost { get; set; }

        /// <summary>
        /// Gets or sets a special price.
        /// </summary>
        public decimal? SpecialPrice { get; set; }

        /// <summary>
        /// Gets or sets the start date of the special price.
        /// </summary>
        public DateTime? SpecialPriceStartDateTimeUtc { get; set; }

        /// <summary>
        /// Gets or sets the end date of the special price.
        /// </summary>
        public DateTime? SpecialPriceEndDateTimeUtc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer can enter a price.
        /// </summary>
        public bool CustomerEntersPrice { get; set; }

        /// <summary>
        /// Gets or sets the minimum price entered by a customer.
        /// </summary>
        public decimal MinimumCustomerEnteredPrice { get; set; }

        /// <summary>
        /// Gets or sets the maximum price entered by a customer.
        /// </summary>
        public decimal MaximumCustomerEnteredPrice { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this product has tier prices configured.
        /// </summary>
        /// <remarks>
        /// We use this property for performance optimization:
        /// if this property is set to false, then we do not need to load TierPrices navigation property.
        /// </remarks>
        public bool HasTierPrices { get; set; }

        /// <summary>
        /// Gets or sets a value for the lowest attribute combination price.
        /// </summary>
        public decimal? LowestAttributeCombinationPrice { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an attribute combination is required for the product to be ordered.
        /// </summary>
        public bool AttributeCombinationRequired { get; set; }

        /// <summary>
        /// Gets or sets the behaviour when selecting product attributes.
        /// </summary>
        public AttributeChoiceBehaviour AttributeChoiceBehaviour { get; set; }

        /// <summary>
        /// Gets or sets the weight.
        /// </summary>
        public decimal Weight { get; set; }

        private decimal _length;
        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        public decimal Length
        {
            [DebuggerStepThrough]
            get => this.GetMergedDataValue(nameof(Length), _length);
            set => _length = value;
        }

        private decimal _width;
        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        public decimal Width
        {
            [DebuggerStepThrough]
            get => this.GetMergedDataValue(nameof(Width), _width);
            set => _width = value;
        }

        private decimal _height;
        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        public decimal Height
        {
            [DebuggerStepThrough]
            get => this.GetMergedDataValue(nameof(Height), _height);
            set => _height = value;
        }

        /// <summary>
        /// Gets or sets the start date from which the product is available.
        /// </summary>
        public DateTime? AvailableStartDateTimeUtc { get; set; }

        /// <summary>
        /// Gets or sets the end date up to which the item is available.
        /// </summary>
        public DateTime? AvailableEndDateTimeUtc { get; set; }

        /// <summary>
        /// Gets or sets a display order. This value is only applied to grouped products when ordering associated products.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the product is published.
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the product has been deleted.
        /// </summary>
        [IgnoreDataMember]
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is a system product.
        /// </summary>
        public bool IsSystemProduct { get; set; }

        /// <summary>
        /// Gets or sets the product system name.
        /// </summary>
        [StringLength(400)]
        public string SystemName { get; set; }

        /// <inheritdoc/>
        public DateTime CreatedOnUtc { get; set; }

        /// <inheritdoc/>
        public DateTime UpdatedOnUtc { get; set; }

        private int? _deliveryTimeId;
        /// <summary>
        /// Gets or sets the delivery time identifier.
        /// </summary>
        public int? DeliveryTimeId
        {
            [DebuggerStepThrough]
            get => this.GetMergedDataValue(nameof(DeliveryTimeId), _deliveryTimeId);
            set => _deliveryTimeId = value;
        }

        private int? _quantityUnitId;
        /// <summary>
        /// Gets or sets the quantity unit identifier.
        /// </summary>
        public int? QuantityUnitId
        {
            [DebuggerStepThrough]
            get => this.GetMergedDataValue(nameof(QuantityUnitId), _quantityUnitId);
            set => _quantityUnitId = value;
        }

        private DeliveryTime _deliveryTime;
        /// <summary>
        /// Gets or sets the delivery time.
        /// </summary>
        public DeliveryTime DeliveryTime
        {
            get => _deliveryTime ?? LazyLoader?.Load(this, ref _deliveryTime);
            set => _deliveryTime = value;
        }

        private QuantityUnit _quantityUnit;
        /// <summary>
        /// Gets or sets the quantity unit.
        /// </summary>
        public QuantityUnit QuantityUnit
        {
            get => _quantityUnit ?? LazyLoader?.Load(this, ref _quantityUnit);
            set => _quantityUnit = value;
        }

        /// <summary>
        /// Gets or sets the customs tariff number.
        /// </summary>
        [StringLength(30)]
        public string CustomsTariffNumber { get; set; }

        /// <summary>
        /// Gets or sets the country of origin identifier.
        /// </summary>
        public int? CountryOfOriginId { get; set; }

        private Country _countryOfOrigin;
        /// <summary>
        /// Gets or sets the country of origin.
        /// </summary>
        public Country CountryOfOrigin
        {
            get => _countryOfOrigin ?? LazyLoader?.Load(this, ref _countryOfOrigin);
            set => _countryOfOrigin = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether base price quotation (PAnGV) is enabled.
        /// </summary>
        public bool BasePriceEnabled { get; set; }

        /// <summary>
        /// Gets or sets the measure unit for the base price (e.g. "kg", "g", "qm²" etc.).
        /// </summary>
        [StringLength(50)]
        public string BasePriceMeasureUnit { get; set; }

        private decimal? _basePriceAmount;
        /// <summary>
        /// Gets or sets the amount of product per packing unit in the given measure unit 
        /// (e.g. 250 ml shower gel: "0.25" if MeasureUnit = "liter" and BaseAmount = 1).
        /// </summary>
        public decimal? BasePriceAmount
        {
            [DebuggerStepThrough]
            get => this.GetMergedDataValue(nameof(BasePriceAmount), _basePriceAmount);
            set => _basePriceAmount = value;
        }

        private int? _basePriceBaseAmount;
        /// <summary>
        /// Gets or sets the reference value for the given measure unit 
        /// (e.g. "1" liter. Formula: [BaseAmount] [MeasureUnit] = [SellingPrice] / [Amount]).
        /// </summary>
        public int? BasePriceBaseAmount
        {
            [DebuggerStepThrough]
            get => this.GetMergedDataValue(nameof(BasePriceBaseAmount), _basePriceBaseAmount);
            set => _basePriceBaseAmount = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the product has a base price.
        /// </summary>
        [NotMapped]
        public bool BasePriceHasValue => BasePriceEnabled && BasePriceAmount.GetValueOrDefault() > 0 && BasePriceBaseAmount.GetValueOrDefault() > 0 && BasePriceMeasureUnit.HasValue();

        /// <summary>
        /// Gets or sets an optional title text of a product bundle.
        /// </summary>
        [StringLength(400)]
        [LocalizedProperty]
        public string BundleTitleText { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether per item shipping of bundle items is enabled.
        /// </summary>
        public bool BundlePerItemShipping { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether per item pricing of bundle items is enabled.
        /// </summary>
        public bool BundlePerItemPricing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether per item shopping cart handling of bundle items is enabled.
        /// </summary>
        public bool BundlePerItemShoppingCart { get; set; }

        /// <summary>
        /// Gets or sets the main picture identifier.
        /// </summary>
        public int? MainPictureId { get; set; }

        /// <summary>
		/// Gets or sets a value indicating whether the product has a preview picture.
		/// </summary>
        public bool HasPreviewPicture { get; set; }

        private ICollection<ProductCategory> _productCategories;
        /// <summary>
        /// Gets or sets the product categories.
        /// </summary>
        public ICollection<ProductCategory> ProductCategories
        {
            get => LazyLoader?.Load(this, ref _productCategories) ?? (_productCategories ??= new HashSet<ProductCategory>());
            protected set => _productCategories = value;
        }

        private ICollection<ProductManufacturer> _productManufacturers;
        /// <summary>
        /// Gets or sets the product manufacturers.
        /// </summary>
        public ICollection<ProductManufacturer> ProductManufacturers
        {
            get => LazyLoader?.Load(this, ref _productManufacturers) ?? (_productManufacturers ??= new HashSet<ProductManufacturer>());
            protected set => _productManufacturers = value;
        }

        private ICollection<ProductMediaFile> _productMediaFiles;
        /// <summary>
        /// Gets or sets the product media files.
        /// </summary>
        public ICollection<ProductMediaFile> ProductMediaFiles
        {
            get => LazyLoader?.Load(this, ref _productMediaFiles) ?? (_productMediaFiles ??= new HashSet<ProductMediaFile>());
            protected set => _productMediaFiles = value;
        }

        private ICollection<ProductReview> _productReviews;
        /// <summary>
        /// Gets or sets the product reviews.
        /// </summary>
        [IgnoreDataMember]
        public ICollection<ProductReview> ProductReviews
        {
            get => LazyLoader?.Load(this, ref _productReviews) ?? (_productReviews ??= new HashSet<ProductReview>());
            protected set => _productReviews = value;
        }

        private ICollection<ProductSpecificationAttribute> _productSpecificationAttributes;
        /// <summary>
        /// Gets or sets the product specification attributes.
        /// </summary>
        public ICollection<ProductSpecificationAttribute> ProductSpecificationAttributes
        {
            get => LazyLoader?.Load(this, ref _productSpecificationAttributes) ?? (_productSpecificationAttributes ??= new HashSet<ProductSpecificationAttribute>());
            protected set => _productSpecificationAttributes = value;
        }

        private ICollection<ProductTag> _productTags;
        /// <summary>
        /// Gets or sets the product tags.
        /// </summary>
        public ICollection<ProductTag> ProductTags
        {
            get => LazyLoader?.Load(this, ref _productTags) ?? (_productTags ??= new HashSet<ProductTag>());
            protected set => _productTags = value;
        }

        private ICollection<ProductVariantAttribute> _productVariantAttributes;
        /// <summary>
        /// Gets or sets the product variant attributes.
        /// </summary>
        public ICollection<ProductVariantAttribute> ProductVariantAttributes
        {
            get => LazyLoader?.Load(this, ref _productVariantAttributes) ?? (_productVariantAttributes ??= new HashSet<ProductVariantAttribute>());
            protected set => _productVariantAttributes = value;
        }

        private ICollection<ProductVariantAttributeCombination> _productVariantAttributeCombinations;
        /// <summary>
        /// Gets or sets the product variant attribute combinations.
        /// </summary>
        public ICollection<ProductVariantAttributeCombination> ProductVariantAttributeCombinations
        {
            get => LazyLoader?.Load(this, ref _productVariantAttributeCombinations) ?? (_productVariantAttributeCombinations ??= new HashSet<ProductVariantAttributeCombination>());
            protected set => _productVariantAttributeCombinations = value;
        }

        private ICollection<TierPrice> _tierPrices;
        /// <summary>
        /// Gets or sets the tier prices.
        /// </summary>
        public ICollection<TierPrice> TierPrices
        {
            get => LazyLoader?.Load(this, ref _tierPrices) ?? (_tierPrices ??= new HashSet<TierPrice>());
            protected set => _tierPrices = value;
        }

        private ICollection<ProductBundleItem> _productBundleItems;
        /// <summary>
        /// Gets or sets the product bundle items.
        /// </summary>
        public ICollection<ProductBundleItem> ProductBundleItems
        {
            get => LazyLoader?.Load(this, ref _productBundleItems) ?? (_productBundleItems ??= new HashSet<ProductBundleItem>());
            protected set => _productBundleItems = value;
        }

        public string GetDisplayName()
        {
            return Name;
        }

        public string[] GetDisplayNameMemberNames() => new[] { nameof(Name) };
    }
}