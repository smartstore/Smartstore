using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Content.Seo;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Diagnostics;

namespace Smartstore.Core.Catalog.Products
{
    public partial class ProductCloner : IProductCloner
    {
        private readonly SmartDbContext _db;
        private readonly IChronometer _chronometer;
        private readonly ILanguageService _languageService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IUrlService _urlService;

        public ProductCloner(
            SmartDbContext db,
            IChronometer chronometer,
            ILanguageService languageService,
            IStoreMappingService storeMappingService,
            IUrlService urlService)
        {
            _db = db;
            _chronometer = chronometer;
            _languageService = languageService;
            _storeMappingService = storeMappingService;
            _urlService = urlService;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual async Task<Product> CloneProductAsync(
            Product product,
            string cloneName,
            bool isPublished,
            bool copyAssociatedProducts = true)
        {
            Guard.NotNull(product, nameof(product));
            Guard.NotEmpty(cloneName, nameof(cloneName));

            using (_chronometer.Step("Copy product " + product.Id))
            {
                Product clone = null;
                var utcNow = DateTime.UtcNow;
                var languages = await _languageService.GetAllLanguagesAsync(true);
                int? sampleDownloadId = null;

                using (var scope = new DbContextScope(_db, autoDetectChanges: false, hooksEnabled: false, deferCommit: true, forceNoTracking: true))
                {
                    if (product.HasSampleDownload && product.SampleDownload != null)
                    {
                        var sampleDownloadClone = product.SampleDownload.Clone();
                        _db.Downloads.Add(sampleDownloadClone);

                        await scope.CommitAsync();
                        sampleDownloadId = sampleDownloadClone.Id;
                    }

                    clone = new Product
                    {
                        ProductTypeId = product.ProductTypeId,
                        ParentGroupedProductId = product.ParentGroupedProductId,
                        Visibility = product.Visibility,
                        Condition = product.Condition,
                        Name = cloneName,
                        ShortDescription = product.ShortDescription,
                        FullDescription = product.FullDescription,
                        ProductTemplateId = product.ProductTemplateId,
                        AdminComment = product.AdminComment,
                        ShowOnHomePage = product.ShowOnHomePage,
                        HomePageDisplayOrder = product.HomePageDisplayOrder,
                        MetaKeywords = product.MetaKeywords,
                        MetaDescription = product.MetaDescription,
                        MetaTitle = product.MetaTitle,
                        AllowCustomerReviews = product.AllowCustomerReviews,
                        LimitedToStores = product.LimitedToStores,
                        Sku = product.Sku,
                        ManufacturerPartNumber = product.ManufacturerPartNumber,
                        Gtin = product.Gtin,
                        IsGiftCard = product.IsGiftCard,
                        GiftCardType = product.GiftCardType,
                        RequireOtherProducts = product.RequireOtherProducts,
                        RequiredProductIds = product.RequiredProductIds,
                        AutomaticallyAddRequiredProducts = product.AutomaticallyAddRequiredProducts,
                        IsDownload = product.IsDownload,
                        UnlimitedDownloads = product.UnlimitedDownloads,
                        MaxNumberOfDownloads = product.MaxNumberOfDownloads,
                        DownloadExpirationDays = product.DownloadExpirationDays,
                        DownloadActivationType = product.DownloadActivationType,
                        HasSampleDownload = product.HasSampleDownload,
                        SampleDownloadId = sampleDownloadId,
                        HasUserAgreement = product.HasUserAgreement,
                        UserAgreementText = product.UserAgreementText,
                        IsRecurring = product.IsRecurring,
                        RecurringCycleLength = product.RecurringCycleLength,
                        RecurringCyclePeriod = product.RecurringCyclePeriod,
                        RecurringTotalCycles = product.RecurringTotalCycles,
                        IsShippingEnabled = product.IsShippingEnabled,
                        IsFreeShipping = product.IsFreeShipping,
                        AdditionalShippingCharge = product.AdditionalShippingCharge,
                        IsEsd = product.IsEsd,
                        IsTaxExempt = product.IsTaxExempt,
                        TaxCategoryId = product.TaxCategoryId,
                        ManageInventoryMethod = product.ManageInventoryMethod,
                        StockQuantity = product.StockQuantity,
                        DisplayStockAvailability = product.DisplayStockAvailability,
                        DisplayStockQuantity = product.DisplayStockQuantity,
                        MinStockQuantity = product.MinStockQuantity,
                        LowStockActivityId = product.LowStockActivityId,
                        NotifyAdminForQuantityBelow = product.NotifyAdminForQuantityBelow,
                        BackorderMode = product.BackorderMode,
                        AllowBackInStockSubscriptions = product.AllowBackInStockSubscriptions,
                        OrderMinimumQuantity = product.OrderMinimumQuantity,
                        OrderMaximumQuantity = product.OrderMaximumQuantity,
                        QuantityStep = product.QuantityStep,
                        QuantiyControlType = product.QuantiyControlType,
                        HideQuantityControl = product.HideQuantityControl,
                        AllowedQuantities = product.AllowedQuantities,
                        DisableBuyButton = product.DisableBuyButton,
                        DisableWishlistButton = product.DisableWishlistButton,
                        AvailableForPreOrder = product.AvailableForPreOrder,
                        CallForPrice = product.CallForPrice,
                        Price = product.Price,
                        OldPrice = product.OldPrice,
                        ProductCost = product.ProductCost,
                        SpecialPrice = product.SpecialPrice,
                        SpecialPriceStartDateTimeUtc = product.SpecialPriceStartDateTimeUtc,
                        SpecialPriceEndDateTimeUtc = product.SpecialPriceEndDateTimeUtc,
                        CustomerEntersPrice = product.CustomerEntersPrice,
                        MinimumCustomerEnteredPrice = product.MinimumCustomerEnteredPrice,
                        MaximumCustomerEnteredPrice = product.MaximumCustomerEnteredPrice,
                        LowestAttributeCombinationPrice = product.LowestAttributeCombinationPrice,
                        AttributeChoiceBehaviour = product.AttributeChoiceBehaviour,
                        Weight = product.Weight,
                        Length = product.Length,
                        Width = product.Width,
                        Height = product.Height,
                        AvailableStartDateTimeUtc = product.AvailableStartDateTimeUtc,
                        AvailableEndDateTimeUtc = product.AvailableEndDateTimeUtc,
                        DisplayOrder = product.DisplayOrder,
                        Published = isPublished,
                        Deleted = product.Deleted,
                        IsSystemProduct = product.IsSystemProduct,
                        DeliveryTimeId = product.DeliveryTimeId,
                        QuantityUnitId = product.QuantityUnitId,
                        BasePriceEnabled = product.BasePriceEnabled,
                        BasePriceMeasureUnit = product.BasePriceMeasureUnit,
                        BasePriceAmount = product.BasePriceAmount,
                        BasePriceBaseAmount = product.BasePriceBaseAmount,
                        BundleTitleText = product.BundleTitleText,
                        BundlePerItemShipping = product.BundlePerItemShipping,
                        BundlePerItemPricing = product.BundlePerItemPricing,
                        BundlePerItemShoppingCart = product.BundlePerItemShoppingCart,
                        CustomsTariffNumber = product.CustomsTariffNumber,
                        CountryOfOriginId = product.CountryOfOriginId,
                        CreatedOnUtc = utcNow,
                        UpdatedOnUtc = utcNow
                    };

                    // Category mappings.
                    foreach (var pc in product.ProductCategories)
                    {
                        clone.ProductCategories.Add(new ProductCategory
                        {
                            CategoryId = pc.CategoryId,
                            IsFeaturedProduct = pc.IsFeaturedProduct,
                            DisplayOrder = pc.DisplayOrder
                        });
                    }

                    // Manufacturer mappings.
                    foreach (var pm in product.ProductManufacturers)
                    {
                        clone.ProductManufacturers.Add(new ProductManufacturer
                        {
                            ManufacturerId = pm.ManufacturerId,
                            IsFeaturedProduct = pm.IsFeaturedProduct,
                            DisplayOrder = pm.DisplayOrder
                        });
                    }

                    // Media file mappings.
                    foreach (var pp in product.ProductPictures)
                    {
                        clone.ProductPictures.Add(new ProductMediaFile
                        {
                            MediaFileId = pp.MediaFileId,
                            DisplayOrder = pp.DisplayOrder
                        });

                        if (!clone.MainPictureId.HasValue)
                        {
                            clone.MainPictureId = pp.MediaFileId;
                        }
                    }

                    // Product specification attributes.
                    foreach (var psa in product.ProductSpecificationAttributes)
                    {
                        clone.ProductSpecificationAttributes.Add(new ProductSpecificationAttribute
                        {
                            SpecificationAttributeOptionId = psa.SpecificationAttributeOptionId,
                            AllowFiltering = psa.AllowFiltering,
                            ShowOnProductPage = psa.ShowOnProductPage,
                            DisplayOrder = psa.DisplayOrder
                        });
                    }

                    // Tier prices.
                    foreach (var tp in product.TierPrices)
                    {
                        clone.TierPrices.Add(new TierPrice
                        {
                            StoreId = tp.StoreId,
                            CustomerRoleId = tp.CustomerRoleId,
                            Quantity = tp.Quantity,
                            Price = tp.Price,
                            CalculationMethod = tp.CalculationMethod
                        });
                        clone.HasTierPrices = true;
                    }

                    // Discount mappings.
                    foreach (var discount in product.AppliedDiscounts)
                    {
                        clone.AppliedDiscounts.Add(discount);
                        clone.HasDiscountsApplied = true;
                    }

                    // Tags.
                    foreach (var tag in product.ProductTags)
                    {
                        clone.ProductTags.Add(tag);
                    }

                    // >>>>>>> Put clone to db (from here on we need the product clone's ID).
                    _db.Products.Add(clone);
                    await scope.CommitAsync();

                    // Related products mappings.
                    // Do not copy mappings of deleted products.
                    var relatedProductsQuery = 
                        from rp in _db.RelatedProducts.AsNoTracking()
                        join p in _db.Products.AsNoTracking() on rp.ProductId2 equals p.Id
                        where rp.ProductId1 == product.Id && !p.Deleted
                        orderby rp.DisplayOrder
                        select rp;
                    var relatedProducts = await relatedProductsQuery.ToListAsync();

                    foreach (var relatedProduct in relatedProducts)
                    {
                        _db.RelatedProducts.Add(new RelatedProduct
                        {
                            ProductId1 = clone.Id,
                            ProductId2 = relatedProduct.ProductId2,
                            DisplayOrder = relatedProduct.DisplayOrder
                        });
                    }

                    // Cross-sell products mappings.
                    var crossSellProductsQuery =
                        from csp in _db.CrossSellProducts.AsNoTracking()
                        join p in _db.Products.AsNoTracking() on csp.ProductId2 equals p.Id
                        where csp.ProductId1 == product.Id && !p.Deleted
                        orderby csp.Id
                        select csp;
                    var crossSellProducts = await crossSellProductsQuery.ToListAsync();

                    foreach (var crossSellProduct in crossSellProducts)
                    {
                        _db.CrossSellProducts.Add(new CrossSellProduct
                        {
                            ProductId1 = clone.Id,
                            ProductId2 = crossSellProduct.ProductId2
                        });
                    }

                    // Store mappings.
                    var selectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(product);
                    foreach (var id in selectedStoreIds)
                    {
                        _storeMappingService.AddStoreMapping(clone, id);
                    }

                    // URL slug.
                    var slugResult = await _urlService.ValidateSlugAsync(clone, clone.Name, true);
                    await _urlService.ApplySlugAsync(slugResult, false);

                    // Localization.


                    // >>>>>>> Our final commit.
                    await scope.CommitAsync();
                }

                return clone;
            }
        }

    }
}
