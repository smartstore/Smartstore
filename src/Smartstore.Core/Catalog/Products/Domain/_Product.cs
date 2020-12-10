using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Products
{
    public class ProductMap : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            // Globally exclude soft-deleted entities from all queries.
            builder.HasQueryFilter(c => !c.Deleted);
        }
    }

    /// <summary>
    /// Represents a product.
    /// </summary>
    /// TODO: (mg) (core): Add all indexes (even old SQL based indexes).
    /// TODO: (mg) (core): Add data annotation attributes.
    /// TODO: (mg) (core): Opt-out (JsonIgnore) data members for API.
    [Index(nameof(Visibility), Name = "IX_Visibility")]
    [Index(nameof(ManufacturerPartNumber), Name = "IX_ManufacturerPartNumber")]
    [Index(nameof(Gtin), Name = "IX_Gtin")]
    [Index(nameof(Deleted), Name = "IX_Deleted")]
    [Index(nameof(Published), nameof(Deleted), nameof(IsSystemProduct), Name = "IX_Product_Published_Deleted_IsSystemProduct")]
    [Index(nameof(SystemName), nameof(IsSystemProduct), Name = "IX_Product_SystemName_IsSystemProduct")]
    public partial class Product : BaseEntity, IAuditable, ISoftDeletable, ILocalizedEntity, ISlugSupported, IAclRestricted, IStoreRestricted, IMergedData
    {
        private readonly ILazyLoader _lazyLoader;

        public Product()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private Product(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the product type identifier.
        /// </summary>
        public int ProductTypeId { get; set; }

        /// <summary>
        /// Gets or sets the parent product identifier. It is used if this product is associated with a grouped product.
        /// </summary>
        public int ParentGroupedProductId { get; set; }

        /// <summary>
        /// Gets or sets the visibility level of the product.
        /// </summary>
        public ProductVisibility Visibility { get; set; }

        /// <summary>
        /// Gets or sets the condition of the product.
        /// </summary>
        public ProductCondition Condition { get; set; }

        /// <summary>
        /// Gets or sets the product name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the short description.
        /// </summary>
        public string ShortDescription { get; set; }

        /// <summary>
        /// Gets or sets the full description.
        /// </summary>
        public string FullDescription { get; set; }

        /// <summary>
        /// Gets or sets the admin comment.
        /// </summary>
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
        public string MetaKeywords { get; set; }

        /// <summary>
        /// Gets or sets the meta description.
        /// </summary>
        public string MetaDescription { get; set; }

        /// <summary>
        /// Gets or sets the meta title.
        /// </summary>
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
        /// Gets or sets a value indicating whether the product requires other products to be added to the cart (product X requires product Y).
        /// </summary>
        public bool RequireOtherProducts { get; set; }

        /// <summary>
        /// Gets or sets the required product identifiers (comma separated).
        /// </summary>
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
        [Obsolete("Since version 3.2 more than one download can be assigned to a product. See property Download.EntityId and Download.EntityName.")]
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
        /// Gets or sets a value indicating whether the product has a sample download file.
        /// </summary>
        public bool HasSampleDownload { get; set; }

        /// <summary>
        /// Gets or sets the sample download identifier.
        /// </summary>
        public int? SampleDownloadId { get; set; }

        private object _sampleDownload;
        /// <summary>
        /// Gets or sets the sample download.
        /// </summary>
        /// TODO: (mg) (core): Implement download entity.
        public object SampleDownload
        {
            get => _lazyLoader?.Load(this, ref _sampleDownload) ?? _sampleDownload;
            set => _sampleDownload = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the product has a user agreement.
        /// </summary>
        public bool HasUserAgreement { get; set; }

        /// <summary>
        /// Gets or sets the license agreement text.
        /// </summary>
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
        /// Gets or sets the recurring total cycles.
        /// </summary>
        public int RecurringTotalCycles { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the product is ship enabled.
        /// </summary>
        public bool IsShipEnabled { get; set; }

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

        private int _stockQuantity;
        /// <summary>
        /// Gets or sets the stock quantity.
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
        public QuantityControlType QuantiyControlType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to hide the quantity input control.
        /// </summary>
        public bool HideQuantityControl { get; set; }

        /// <summary>
        /// Gets or sets the comma seperated list of allowed quantities. If <c>null</c> or empty then any quantity is allowed.
        /// </summary>
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
        /// Gets or sets the product price.
        /// </summary>
        public decimal Price
        {
            [DebuggerStepThrough]
            get => this.GetMergedDataValue(nameof(Price), _price);
            set => _price = value;
        }

        /// <summary>
        /// Gets or sets the former price.
        /// </summary>
        public decimal OldPrice { get; set; }

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
        /// Gets or sets the behaviour when selecting product attributes.
        /// </summary>
        public AttributeChoiceBehaviour AttributeChoiceBehaviour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this product has discounts applied.
        /// </summary>
        /// <remarks>
        /// We use this property for performance optimization:
        /// if this property is set to false, then we do not need to load AppliedDiscounts navigation property.
        /// </remarks>
        public bool HasDiscountsApplied { get; set; }

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
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is a system product.
        /// </summary>
        public bool IsSystemProduct { get; set; }

        /// <summary>
        /// Gets or sets the product system name.
        /// </summary>
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



        /// <inheritdoc/>
        public bool MergedDataIgnore { get; set; }

        /// <inheritdoc/>
        public Dictionary<string, object> MergedDataValues { get; set; }

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