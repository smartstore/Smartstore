using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
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
                var clone = new Product();
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

                    var props = FastProperty.GetProperties(typeof(Product), PropertyCachingStrategy.EagerCached);

                    foreach (var prop in props.Values)
                    {
                        if (prop.IsComplexType)
                            continue;

                        if (!prop.IsPublicSettable)
                            continue;

                        prop.SetValue(clone, prop.GetValue(product));
                    }

                    clone.Id = 0;
                    clone.Name = cloneName;
                    clone.SampleDownloadId = sampleDownloadId;
                    clone.Published = isPublished;
                    clone.CreatedOnUtc = utcNow;
                    clone.UpdatedOnUtc = utcNow;

                    // Category mappings.
                    clone.ProductCategories.AddRange(product.ProductCategories.Select(x => new ProductCategory
                    {
                        CategoryId = x.CategoryId,
                        IsFeaturedProduct = x.IsFeaturedProduct,
                        DisplayOrder = x.DisplayOrder
                    }));

                    // Manufacturer mappings.
                    clone.ProductManufacturers.AddRange(product.ProductManufacturers.Select(x => new ProductManufacturer
                    {
                        ManufacturerId = x.ManufacturerId,
                        IsFeaturedProduct = x.IsFeaturedProduct,
                        DisplayOrder = x.DisplayOrder
                    }));

                    // Media file mappings.
                    clone.ProductPictures.AddRange(product.ProductPictures.Select(x => new ProductMediaFile
                    {
                        MediaFileId = x.MediaFileId,
                        DisplayOrder = x.DisplayOrder
                    }));
                    if (clone.MainPictureId == null)
                    {
                        clone.MainPictureId = product.ProductPictures.FirstOrDefault()?.MediaFileId;
                    }

                    // Product specification attributes.
                    clone.ProductSpecificationAttributes.AddRange(product.ProductSpecificationAttributes.Select(x => new ProductSpecificationAttribute
                    {
                        SpecificationAttributeOptionId = x.SpecificationAttributeOptionId,
                        AllowFiltering = x.AllowFiltering,
                        ShowOnProductPage = x.ShowOnProductPage,
                        DisplayOrder = x.DisplayOrder
                    }));

                    // Tier prices.
                    clone.TierPrices.AddRange(product.TierPrices.Select(x => new TierPrice
                    {
                        StoreId = x.StoreId,
                        CustomerRoleId = x.CustomerRoleId,
                        Quantity = x.Quantity,
                        Price = x.Price,
                        CalculationMethod = x.CalculationMethod
                    }));
                    clone.HasTierPrices = clone.TierPrices.Any();

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
                    var relatedProductsQuery = 
                        from rp in _db.RelatedProducts.AsNoTracking()
                        join p in _db.Products.AsNoTracking() on rp.ProductId2 equals p.Id
                        where rp.ProductId1 == product.Id
                        orderby rp.DisplayOrder
                        select rp;
                    
                    var relatedProducts = await relatedProductsQuery.ToListAsync();
                    _db.RelatedProducts.AddRange(relatedProducts.Select(x => new RelatedProduct
                    {
                        ProductId1 = clone.Id,
                        ProductId2 = x.ProductId2,
                        DisplayOrder = x.DisplayOrder
                    }));

                    // Cross-sell products mappings.
                    var crossSellProductsQuery =
                        from csp in _db.CrossSellProducts.AsNoTracking()
                        join p in _db.Products.AsNoTracking() on csp.ProductId2 equals p.Id
                        where csp.ProductId1 == product.Id
                        orderby csp.Id
                        select csp;

                    var crossSellProducts = await crossSellProductsQuery.ToListAsync();
                    _db.CrossSellProducts.AddRange(crossSellProducts.Select(x => new CrossSellProduct
                    {
                        ProductId1 = clone.Id,
                        ProductId2 = x.ProductId2
                    }));

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