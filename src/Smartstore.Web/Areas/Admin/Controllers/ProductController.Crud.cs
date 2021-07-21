using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using AngleSharp.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.Admin.Models.Catalog;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.DataGrid;
using Smartstore.Web.TagHelpers.Shared;
namespace Smartstore.Admin.Controllers
{
    public partial class ProductController : AdminControllerBase
    {
        #region Product list / create / edit / delete

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> List(ProductListModel model)
        {
            model.DisplayProductPictures = _adminAreaSettings.DisplayProductPictures;
            model.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();

            // TODO: (mc) (core) Remove if not needed anymore.
            //foreach (var c in (await _categoryService.GetCategoryTreeAsync(includeHidden: true)).FlattenNodes(false))
            //{
            //    model.AvailableCategories.Add(new SelectListItem { Text = c.GetCategoryNameIndented(), Value = c.Id.ToString() });
            //}

            //foreach (var m in await _db.Manufacturers.AsNoTracking().ApplyStandardFilter(true).Select(x => new { x.Name, x.Id }).ToListAsync())
            //{
            //    model.AvailableManufacturers.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString() });
            //}

            //model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList(false).ToList();

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductList(GridCommand command, ProductListModel model)
        {
            var gridModel = new GridModel<ProductOverviewModel>();

            var fields = new List<string> { "name" };
            if (_searchSettings.SearchFields.Contains("sku"))
            {
                fields.Add("sku");
            }

            if (_searchSettings.SearchFields.Contains("shortdescription"))
            {
                fields.Add("shortdescription");
            }

            var searchQuery = new CatalogSearchQuery(fields.ToArray(), model.SearchProductName)
                .HasStoreId(model.SearchStoreId)
                .WithCurrency(_workContext.WorkingCurrency)
                .WithLanguage(_workContext.WorkingLanguage);

            if (model.SearchIsPublished.HasValue)
            {
                searchQuery = searchQuery.PublishedOnly(model.SearchIsPublished.Value);
            }

            if (model.SearchHomePageProducts.HasValue)
            {
                searchQuery = searchQuery.HomePageProductsOnly(model.SearchHomePageProducts.Value);
            }

            if (model.SearchProductTypeId > 0)
            {
                searchQuery = searchQuery.IsProductType((ProductType)model.SearchProductTypeId);
            }

            if (model.SearchWithoutManufacturers.HasValue)
            {
                searchQuery = searchQuery.HasAnyManufacturer(!model.SearchWithoutManufacturers.Value);
            }
            else if (model.SearchManufacturerId != 0)
            {
                searchQuery = searchQuery.WithManufacturerIds(null, model.SearchManufacturerId);
            }


            if (model.SearchWithoutCategories.HasValue)
            {
                searchQuery = searchQuery.HasAnyCategory(!model.SearchWithoutCategories.Value);
            }
            else if (model.SearchCategoryId != 0)
            {
                searchQuery = searchQuery.WithCategoryIds(null, model.SearchCategoryId);
            }

            IPagedList<Product> products;

            if (_searchSettings.UseCatalogSearchInBackend)
            {
                searchQuery = searchQuery.Slice((command.Page - 1) * command.PageSize, command.PageSize);

                var sort = command.Sorting?.FirstOrDefault();
                if (sort != null)
                {
                    switch (sort.Member)
                    {
                        case nameof(ProductModel.Name):
                            searchQuery = searchQuery.SortBy(sort.Descending ? ProductSortingEnum.NameDesc : ProductSortingEnum.NameAsc);
                            break;
                        case nameof(ProductModel.Price):
                            searchQuery = searchQuery.SortBy(sort.Descending ? ProductSortingEnum.PriceDesc : ProductSortingEnum.PriceAsc);
                            break;
                        case nameof(ProductModel.CreatedOn):
                            searchQuery = searchQuery.SortBy(sort.Descending ? ProductSortingEnum.CreatedOn : ProductSortingEnum.CreatedOnAsc);
                            break;
                    }
                }

                if (!searchQuery.Sorting.Any())
                {
                    searchQuery = searchQuery.SortBy(ProductSortingEnum.NameAsc);
                }

                var searchResult = await _catalogSearchService.Value.SearchAsync(searchQuery);
                products = await searchResult.GetHitsAsync();
            }
            else
            {
                var query = _catalogSearchService.Value
                    .PrepareQuery(searchQuery)
                    .ApplyGridCommand(command, false);

                products = await query.ToPagedList(command).LoadAsync();
            }

            var fileIds = products.AsEnumerable()
                .Select(x => x.MainPictureId ?? 0)
                .Where(x => x != 0)
                .Distinct()
                .ToArray();

            var files = (await _mediaService.GetFilesByIdsAsync(fileIds)).ToDictionarySafe(x => x.Id);

            gridModel.Rows = products.AsEnumerable().Select(x =>
            {
                var productModel = new ProductOverviewModel
                {
                    Sku = x.Sku,
                    Published = x.Published,
                    ProductTypeLabelHint = x.ProductTypeLabelHint,
                    Name = x.Name,
                    Id = x.Id,
                    StockQuantity = x.StockQuantity,
                    Price = x.Price,
                    LimitedToStores = x.LimitedToStores,
                    EditUrl = Url.Action("Edit", "Product", new { id = x.Id }),
                    ManufacturerPartNumber = x.ManufacturerPartNumber,
                    Gtin = x.Gtin,
                    MinStockQuantity = x.MinStockQuantity,
                    OldPrice = x.OldPrice,
                    AvailableStartDateTimeUtc = x.AvailableStartDateTimeUtc,
                    AvailableEndDateTimeUtc = x.AvailableEndDateTimeUtc
                };

                //MiniMapper.Map(x, productModel);

                files.TryGetValue(x.MainPictureId ?? 0, out var file);

                // TODO: (core) Use IImageModel
                productModel.PictureThumbnailUrl = _mediaService.GetUrl(file, _mediaSettings.CartThumbPictureSize);
                productModel.NoThumb = file == null;

                productModel.ProductTypeName = x.GetProductTypeLabel(Services.Localization);
                productModel.UpdatedOn = _dateTimeHelper.ConvertToUserTime(x.UpdatedOnUtc, DateTimeKind.Utc);
                productModel.CreatedOn = _dateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);
                productModel.CopyProductModel.Name = T("Admin.Common.CopyOf", x.Name);

                return productModel;
            });

            gridModel.Total = products.TotalCount;

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Delete)]
        public async Task<IActionResult> ProductDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                var toDelete = await _db.Products
                    .AsQueryable()
                    .Where(x => ids.Contains(x.Id))
                    .ToListAsync();

                numDeleted = toDelete.Count;

                _db.Products.RemoveRange(toDelete);
                await _db.SaveChangesAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FindByIdAsync(id);
            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteProduct, T("ActivityLog.DeleteProduct"), product.Name);

            NotifySuccess(T("Admin.Catalog.Products.Deleted"));
            return RedirectToAction("List");
        }

        [Permission(Permissions.Catalog.Product.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new ProductModel();
            await PrepareProductModelAsync(model, null, true, true);
            AddLocales(model.Locales);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Catalog.Product.Create)]
        public async Task<IActionResult> Create(ProductModel model, bool continueEditing, IFormCollection form)
        {
            if (model.DownloadFileVersion.HasValue() && model.DownloadId != null)
            {
                try
                {
                    var test = SemanticVersion.Parse(model.DownloadFileVersion);
                }
                catch
                {
                    ModelState.AddModelError("FileVersion", T("Admin.Catalog.Products.Download.SemanticVersion.NotValid"));
                }
            }

            if (ModelState.IsValid)
            {
                var product = new Product();

                _ = await MapModelToProductAsync(model, product, form);

                product.StockQuantity = 10000;
                product.OrderMinimumQuantity = 1;
                product.OrderMaximumQuantity = 100;
                product.HideQuantityControl = false;
                product.IsShippingEnabled = true;
                product.AllowCustomerReviews = true;
                product.Published = true;
                product.MaximumCustomerEnteredPrice = 1000;

                if (product.ProductType == ProductType.BundledProduct)
                {
                    product.BundleTitleText = T("Products.Bundle.BundleIncludes");
                }

                _db.Products.Add(product);
                await _db.SaveChangesAsync();

                await UpdateDataOfExistingProductAsync(product, model, false, false);

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.AddNewProduct, T("ActivityLog.AddNewProduct"), product.Name);

                if (continueEditing)
                {
                    // ensure that the same tab gets selected in edit view
                    var selectedTab = TempData["SelectedTab.product-edit"] as SelectedTabInfo;
                    if (selectedTab != null)
                    {
                        selectedTab.Path = Url.Action("Edit", new RouteValueDictionary { { "id", product.Id } });
                    }
                }

                NotifySuccess(T("Admin.Catalog.Products.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = product.Id }) : RedirectToAction("List");
            }

            // If we got this far something failed. Redisplay form.
            await PrepareProductModelAsync(model, null, false, true);

            return View(model);
        }

        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products
                .Include(x => x.ProductTags)
                .Include(x => x.AppliedDiscounts)
                .FindByIdAsync(id);

            if (product == null)
            {
                NotifyWarning(T("Products.NotFound", id));
                return RedirectToAction("List");
            }

            if (product.Deleted)
            {
                NotifyWarning(T("Products.Deleted", id));
                return RedirectToAction("List");
            }

            var model = await MapperFactory.MapAsync<Product, ProductModel>(product);
            await PrepareProductModelAsync(model, product, false, false);

            AddLocales(model.Locales, async (locale, languageId) =>
            {
                locale.Name = product.GetLocalized(x => x.Name, languageId, false, false);
                locale.ShortDescription = product.GetLocalized(x => x.ShortDescription, languageId, false, false);
                locale.FullDescription = product.GetLocalized(x => x.FullDescription, languageId, false, false);
                locale.MetaKeywords = product.GetLocalized(x => x.MetaKeywords, languageId, false, false);
                locale.MetaDescription = product.GetLocalized(x => x.MetaDescription, languageId, false, false);
                locale.MetaTitle = product.GetLocalized(x => x.MetaTitle, languageId, false, false);
                locale.SeName = await product.GetActiveSlugAsync(languageId, false, false);
                locale.BundleTitleText = product.GetLocalized(x => x.BundleTitleText, languageId, false, false);
            });

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Catalog.Product.Update)]
        public async Task<IActionResult> Edit(ProductModel model, bool continueEditing, IFormCollection form)
        {
            var product = await _db.Products
                .Include(x => x.AppliedDiscounts)
                .Include(x => x.ProductTags)
                .FindByIdAsync(model.Id);

            if (product == null)
            {
                NotifyWarning(T("Products.NotFound", model.Id));
                return RedirectToAction("List");
            }

            if (product.Deleted)
            {
                NotifyWarning(T("Products.Deleted", model.Id));
                return RedirectToAction("List");
            }

            await UpdateDataOfProductDownloadsAsync(model);

            if (ModelState.IsValid)
            {
                var nameChanged = await MapModelToProductAsync(model, product, form);
                await UpdateDataOfExistingProductAsync(product, model, true, nameChanged);

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditProduct, T("ActivityLog.EditProduct"), product.Name);

                NotifySuccess(T("Admin.Catalog.Products.Updated"));
                return continueEditing ? RedirectToAction("Edit", new { id = product.Id }) : RedirectToAction("List");
            }

            // If we got this far something failed. Redisplay form.
            await PrepareProductModelAsync(model, product, false, true);

            return View(model);
        }

        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> LoadEditTab(int id, string tabName, string viewPath = null)
        {
            try
            {
                if (id == 0)
                {
                    // If id is 0 we're in create mode.
                    return PartialView("_Create.SaveFirst");
                }

                if (tabName.IsEmpty())
                {
                    return Content("A unique tab name has to specified (route parameter: tabName)");
                }

                var product = await _db.Products
                    .Include(x => x.AppliedDiscounts)
                    .Include(x => x.ProductTags)
                    .FindByIdAsync(id, false);

                var model = await MapperFactory.MapAsync<Product, ProductModel>(product);

                await PrepareProductModelAsync(model, product, false, false);

                AddLocales(model.Locales, async (locale, languageId) =>
                {
                    locale.Name = product.GetLocalized(x => x.Name, languageId, false, false);
                    locale.ShortDescription = product.GetLocalized(x => x.ShortDescription, languageId, false, false);
                    locale.FullDescription = product.GetLocalized(x => x.FullDescription, languageId, false, false);
                    locale.MetaKeywords = product.GetLocalized(x => x.MetaKeywords, languageId, false, false);
                    locale.MetaDescription = product.GetLocalized(x => x.MetaDescription, languageId, false, false);
                    locale.MetaTitle = product.GetLocalized(x => x.MetaTitle, languageId, false, false);
                    locale.SeName = await product.GetActiveSlugAsync(languageId, false, false);
                    locale.BundleTitleText = product.GetLocalized(x => x.BundleTitleText, languageId, false, false);
                });

                return PartialView(viewPath.NullEmpty() ?? "_CreateOrUpdate." + tabName, model);
            }
            catch (Exception ex)
            {
                return Content("Error while loading template: " + ex.Message);
            }
        }

        [HttpPost, ActionName("List")]
        [FormValueRequired("go-to-product-by-sku")]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> GoToSku(ProductListModel model)
        {
            var sku = model.GoDirectlyToSku;

            if (sku.HasValue())
            {
                var product = await _db.Products
                    .ApplySkuFilter(sku)
                    .Select(x => new { x.Id })
                    .FirstOrDefaultAsync();

                if (product != null)
                {
                    return RedirectToAction("Edit", "Product", new { id = product.Id });
                }

                var combination = await _db.ProductVariantAttributeCombinations
                    .AsNoTracking()
                    .ApplySkuFilter(sku)
                    .Select(x => new { x.ProductId, ProductDeleted = x.Product.Deleted })
                    .FirstOrDefaultAsync();

                if (combination != null)
                {
                    return RedirectToAction("Edit", "Product", new { id = combination.ProductId });
                }
            }

            // Not found.
            return RedirectToAction("List");
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Create)]
        public async Task<IActionResult> CopyProduct(ProductModel model)
        {
            var copyModel = model.CopyProductModel;
            try
            {
                Product newProduct = null;
                // Lets just load this untracked as nearly all navigation properties are needed in order to copy successfully.
                // We just eager load the most common properties.
                var product = await _db.Products
                    .Include(x => x.ProductCategories)
                    .Include(x => x.ProductManufacturers)
                    .Include(x => x.ProductSpecificationAttributes)
                    .Include(x => x.ProductVariantAttributes)
                    .Include(x => x.ProductVariantAttributeCombinations)
                    .FindByIdAsync(copyModel.Id);

                for (var i = 1; i <= copyModel.NumberOfCopies; ++i)
                {
                    var newName = copyModel.NumberOfCopies > 1 ? $"{copyModel.Name} {i}" : copyModel.Name;
                    newProduct = await _productCloner.Value.CloneProductAsync(product, newName, copyModel.Published);
                }

                if (newProduct != null)
                {
                    NotifySuccess(T("Admin.Common.TaskSuccessfullyProcessed"));
                    return RedirectToAction("Edit", new { id = newProduct.Id });
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                NotifyError(ex.ToAllMessages());
            }

            return RedirectToAction("Edit", new { id = copyModel.Id });
        }

        #endregion

        #region Utitilies

        private async Task PrepareProductModelAsync(ProductModel model, Product product, bool setPredefinedValues, bool excludeProperties)
        {
            Guard.NotNull(model, nameof(model));

            if (product != null)
            {
                var parentGroupedProduct = await _db.Products.FindByIdAsync(product.ParentGroupedProductId, false);
                if (parentGroupedProduct != null)
                {
                    model.AssociatedToProductId = product.ParentGroupedProductId;
                    model.AssociatedToProductName = parentGroupedProduct.Name;
                }

                model.CreatedOn = _dateTimeHelper.ConvertToUserTime(product.CreatedOnUtc, DateTimeKind.Utc);
                model.UpdatedOn = _dateTimeHelper.ConvertToUserTime(product.UpdatedOnUtc, DateTimeKind.Utc);
                model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(product);
                model.SelectedCustomerRoleIds = await _aclService.GetAuthorizedCustomerRoleIdsAsync(product);
                model.OriginalStockQuantity = product.StockQuantity;

                if (product.LimitedToStores)
                {
                    var storeMappings = await _storeMappingService.GetStoreMappingCollectionAsync(nameof(Product), new[] { product.Id });
                    var currentStoreId = Services.StoreContext.CurrentStore.Id;

                    if (storeMappings.FirstOrDefault(x => x.StoreId == currentStoreId) == null)
                    {
                        var storeMapping = storeMappings.FirstOrDefault();
                        if (storeMapping != null)
                        {
                            var store = Services.StoreContext.GetStoreById(storeMapping.StoreId);
                            if (store != null)
                                model.ProductUrl = store.Url.EnsureEndsWith("/") + await product.GetActiveSlugAsync();
                        }
                    }
                }

                if (model.ProductUrl.IsEmpty())
                {
                    model.ProductUrl = Url.RouteUrl("Product", new { SeName = await product.GetActiveSlugAsync() }, Request.Scheme);
                }

                // Downloads.
                var productDownloads = await _db.Downloads
                    .AsNoTracking()
                    .ApplyEntityFilter(product)
                    .ApplyVersionFilter(string.Empty)
                    .ToListAsync();

                model.DownloadVersions = productDownloads
                    .Select(x => new DownloadVersion
                    {
                        FileVersion = x.FileVersion,
                        DownloadId = x.Id,
                        FileName = x.UseDownloadUrl ? x.DownloadUrl : x.MediaFile?.Name,
                        DownloadUrl = x.UseDownloadUrl ? x.DownloadUrl : Url.Action("DownloadFile", "Download", new { downloadId = x.Id })
                    })
                    .ToList();

                var currentDownload = productDownloads.FirstOrDefault();

                model.DownloadId = currentDownload?.Id;
                model.CurrentDownload = currentDownload;
                if (currentDownload != null && currentDownload.MediaFile != null)
                {
                    model.DownloadThumbUrl = await _mediaService.GetUrlAsync(currentDownload.MediaFile.Id, _mediaSettings.CartThumbPictureSize, null, true);
                    currentDownload.DownloadUrl = Url.Action("DownloadFile", "Download", new { downloadId = currentDownload.Id });
                }

                model.DownloadFileVersion = (currentDownload?.FileVersion).EmptyNull();
                model.OldSampleDownloadId = model.SampleDownloadId;

                // Media files.
                var file = await _mediaService.GetFileByIdAsync(product.MainPictureId ?? 0);
                model.PictureThumbnailUrl = _mediaService.GetUrl(file, _mediaSettings.CartThumbPictureSize);
                model.NoThumb = file == null;

                await PrepareProductPictureModelAsync(model);
                model.AddPictureModel.PictureId = product.MainPictureId ?? 0;
            }

            model.PrimaryStoreCurrencyCode = Services.StoreContext.CurrentStore.PrimaryStoreCurrency.CurrencyCode;

            var measure = await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId, false);
            var dimension = await _db.MeasureDimensions.FindByIdAsync(_measureSettings.BaseDimensionId, false);

            model.BaseWeightIn = measure?.GetLocalized(x => x.Name) ?? string.Empty;
            model.BaseDimensionIn = dimension?.GetLocalized(x => x.Name) ?? string.Empty;

            model.NumberOfAvailableProductAttributes = await _db.ProductAttributes.CountAsync();
            model.NumberOfAvailableManufacturers = await _db.Manufacturers.CountAsync();
            model.NumberOfAvailableCategories = await _db.Categories.CountAsync();

            // Copy product.
            if (product != null)
            {
                model.CopyProductModel.Id = product.Id;
                model.CopyProductModel.Name = T("Admin.Common.CopyOf", product.Name);
                model.CopyProductModel.Published = true;
            }

            // Templates.
            var templates = await _db.ProductTemplates
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            ViewBag.AvailableProductTemplates = new List<SelectListItem>();
            foreach (var template in templates)
            {
                ViewBag.AvailableProductTemplates.Add(new SelectListItem
                {
                    Text = template.Name,
                    Value = template.Id.ToString()
                });
            }

            // Product tags.
            if (product != null)
            {
                model.ProductTags = product.ProductTags.Select(x => x.Name).ToArray();
            }

            var allTags = await _db.ProductTags
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToListAsync();

            ViewBag.AvailableProductTags = new MultiSelectList(allTags, model.ProductTags);

            // Tax categories.
            var taxCategories = await _db.TaxCategories
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            ViewBag.AvailableTaxCategories = new List<SelectListItem>();
            foreach (var tc in taxCategories)
            {
                ViewBag.AvailableTaxCategories.Add(new SelectListItem
                {
                    Text = tc.Name,
                    Value = tc.Id.ToString(),
                    Selected = product != null && !setPredefinedValues && tc.Id == product.TaxCategoryId
                });
            }

            // Do not pre-select a tax category that is not stored.
            if (product != null && product.TaxCategoryId == 0)
            {
                ViewBag.AvailableTaxCategories.Insert(0, new SelectListItem { Text = T("Common.PleaseSelect"), Value = string.Empty, Selected = true });
            }

            // Delivery times.
            if (setPredefinedValues)
            {
                var defaultDeliveryTime = await _db.DeliveryTimes
                    .AsNoTracking()
                    .Where(x => x.IsDefault == true)
                    .FirstOrDefaultAsync();

                model.DeliveryTimeId = defaultDeliveryTime?.Id;
            }

            // Quantity units.
            var quantityUnits = await _db.QuantityUnits
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            ViewBag.AvailableQuantityUnits = new List<SelectListItem>();
            foreach (var mu in quantityUnits)
            {
                ViewBag.AvailableQuantityUnits.Add(new SelectListItem
                {
                    Text = mu.Name,
                    Value = mu.Id.ToString(),
                    Selected = product != null && !setPredefinedValues && mu.Id == product.QuantityUnitId.GetValueOrDefault()
                });
            }

            // BasePrice aka PAnGV
            var measureUnitKeys = await _db.MeasureWeights.AsNoTracking().OrderBy(x => x.DisplayOrder).Select(x => x.SystemKeyword).ToListAsync();
            var measureDimensionKeys = await _db.MeasureDimensions.AsNoTracking().OrderBy(x => x.DisplayOrder).Select(x => x.SystemKeyword).ToListAsync();
            var measureUnits = new HashSet<string>(measureUnitKeys.Concat(measureDimensionKeys), StringComparer.OrdinalIgnoreCase);

            // Don't forget biz import!
            if (product != null && !setPredefinedValues && product.BasePriceMeasureUnit.HasValue())
            {
                measureUnits.Add(product.BasePriceMeasureUnit);
            }

            ViewBag.AvailableMeasureUnits = new List<SelectListItem>();
            foreach (var mu in measureUnits)
            {
                ViewBag.AvailableMeasureUnits.Add(new SelectListItem
                {
                    Text = mu,
                    Value = mu,
                    Selected = product != null && !setPredefinedValues && mu.EqualsNoCase(product.BasePriceMeasureUnit)
                });
            }

            // Specification attributes.
            // TODO: (mh) (core) We can't do this!!! The list can be very large. This needs to be AJAXified. TBD with MC.
            var specificationAttributes = await _db.SpecificationAttributes
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            var availableAttributes = new List<SelectListItem>();
            var availableOptions = new List<SelectListItem>();
            for (int i = 0; i < specificationAttributes.Count; i++)
            {
                var sa = specificationAttributes[i];
                availableAttributes.Add(new SelectListItem { Text = sa.Name, Value = sa.Id.ToString() });
                if (i == 0)
                {
                    var options = await _db.SpecificationAttributeOptions
                        .AsNoTracking()
                        .Where(x => x.SpecificationAttributeId == sa.Id)
                        .OrderBy(x => x.DisplayOrder)
                        .ToListAsync();

                    // Attribute options.
                    foreach (var sao in options)
                    {
                        availableOptions.Add(new SelectListItem { Text = sao.Name, Value = sao.Id.ToString() });
                    }
                }
            }

            ViewBag.AvailableAttributes = availableAttributes;
            ViewBag.AvailableOptions = availableOptions;

            if (product != null && !excludeProperties)
            {
                model.SelectedDiscountIds = product.AppliedDiscounts.Select(d => d.Id).ToArray();
            }

            var inventoryMethods = ((ManageInventoryMethod[])Enum.GetValues(typeof(ManageInventoryMethod))).Where(
                x => model.ProductTypeId != (int)ProductType.BundledProduct || x != ManageInventoryMethod.ManageStockByAttributes
            );

            ViewBag.AvailableManageInventoryMethods = new List<SelectListItem>();
            foreach (var inventoryMethod in inventoryMethods)
            {
                ViewBag.AvailableManageInventoryMethods.Add(new SelectListItem
                {
                    Value = ((int)inventoryMethod).ToString(),
                    Text = inventoryMethod.GetLocalizedEnum(),
                    Selected = ((int)inventoryMethod == model.ManageInventoryMethodId)
                });
            }

            ViewBag.AvailableCountries = await _db.Countries.AsNoTracking().ApplyStandardFilter(true)
                .Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString(),
                    Selected = product != null && x.Id == product.CountryOfOriginId
                })
                .ToListAsync();

            if (setPredefinedValues)
            {
                // TODO: These should be hidden settings.
                model.MaximumCustomerEnteredPrice = 1000;
                model.MaxNumberOfDownloads = 10;
                model.RecurringCycleLength = 100;
                model.RecurringTotalCycles = 10;
                model.StockQuantity = 10000;
                model.NotifyAdminForQuantityBelow = 1;
                model.OrderMinimumQuantity = 1;
                model.OrderMaximumQuantity = 100;
                model.QuantityStep = 1;
                model.HideQuantityControl = false;
                model.UnlimitedDownloads = true;
                model.IsShippingEnabled = true;
                model.AllowCustomerReviews = true;
                model.Published = true;
                model.HasPreviewPicture = false;
            }
        }

        private async Task PrepareProductPictureModelAsync(ProductModel model)
        {
            Guard.NotNull(model, nameof(model));

            var productPictures = await _db.ProductMediaFiles
                .AsNoTracking()
                .Include(x => x.MediaFile)
                .ApplyProductFilter(model.Id)
                .ToListAsync();

            model.ProductMediaFiles = productPictures
                .Select(x =>
                {
                    var media = new ProductMediaFile
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
                        MediaFileId = x.MediaFileId,
                        DisplayOrder = x.DisplayOrder,
                        MediaFile = x.MediaFile
                    };

                    return media;
                })
                .ToList();
        }

        private async Task PrepareBundleItemEditModelAsync(ProductBundleItemModel model, ProductBundleItem bundleItem, string btnId, string formId, bool refreshPage = false)
        {
            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;
            ViewBag.RefreshPage = refreshPage;

            if (bundleItem == null)
            {
                ViewBag.Title = T("Admin.Catalog.Products.BundleItems.EditOf").Value;
                return;
            }

            model.CreatedOn = _dateTimeHelper.ConvertToUserTime(bundleItem.CreatedOnUtc, DateTimeKind.Utc);
            model.UpdatedOn = _dateTimeHelper.ConvertToUserTime(bundleItem.UpdatedOnUtc, DateTimeKind.Utc);
            model.IsPerItemPricing = bundleItem.BundleProduct.BundlePerItemPricing;

            if (model.Locales.Count == 0)
            {
                AddLocales(model.Locales, (locale, languageId) =>
                {
                    locale.Name = bundleItem.GetLocalized(x => x.Name, languageId, false, false);
                    locale.ShortDescription = bundleItem.GetLocalized(x => x.ShortDescription, languageId, false, false);
                });
            }

            ViewBag.Title = $"{T("Admin.Catalog.Products.BundleItems.EditOf")} {bundleItem.Product.Name} ({bundleItem.Product.Sku})";

            var attributes = await _db.ProductVariantAttributes
                .AsNoTracking()
                .Include(x => x.ProductAttribute)
                .ApplyProductFilter(new[] { bundleItem.ProductId })
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            foreach (var attribute in attributes)
            {
                var attributeModel = new ProductBundleItemAttributeModel()
                {
                    Id = attribute.Id,
                    Name = attribute.ProductAttribute.Alias.HasValue() ? $"{attribute.ProductAttribute.Name} ({attribute.ProductAttribute.Alias})" : attribute.ProductAttribute.Name
                };

                var attributeValues = await _db.ProductVariantAttributeValues
                    .AsNoTracking()
                    .OrderBy(x => x.DisplayOrder)
                    .Where(x => x.ProductVariantAttributeId == attribute.Id)
                    .ToListAsync();

                foreach (var attributeValue in attributeValues)
                {
                    var filteredValue = bundleItem.AttributeFilters.FirstOrDefault(x => x.AttributeId == attribute.Id && x.AttributeValueId == attributeValue.Id);

                    attributeModel.Values.Add(new SelectListItem()
                    {
                        Text = attributeValue.Name,
                        Value = attributeValue.Id.ToString(),
                        Selected = (filteredValue != null)
                    });

                    if (filteredValue != null)
                    {
                        attributeModel.PreSelect.Add(new SelectListItem()
                        {
                            Text = attributeValue.Name,
                            Value = attributeValue.Id.ToString(),
                            Selected = filteredValue.IsPreSelected
                        });
                    }
                }

                if (attributeModel.Values.Count > 0)
                {
                    if (attributeModel.PreSelect.Count > 0)
                    {
                        attributeModel.PreSelect.Insert(0, new SelectListItem() { Text = T("Admin.Common.PleaseSelect") });
                    }

                    model.Attributes.Add(attributeModel);
                }
            }
        }

        private async Task SaveFilteredAttributesAsync(ProductBundleItem bundleItem)
        {
            var form = Request.Form;

            var toDelete = await _db.ProductBundleItemAttributeFilter
                .Where(x => x.BundleItemId == bundleItem.Id)
                .ToListAsync();

            _db.ProductBundleItemAttributeFilter.RemoveRange(toDelete);
            await _db.SaveChangesAsync();

            var allFilterKeys = form.Keys.Where(x => x.HasValue() && x.StartsWith(ProductBundleItemAttributeModel.AttributeControlPrefix));

            foreach (var key in allFilterKeys)
            {
                int attributeId = key[ProductBundleItemAttributeModel.AttributeControlPrefix.Length..].ToInt();
                string preSelectId = form[ProductBundleItemAttributeModel.PreSelectControlPrefix + attributeId.ToString()].ToString().EmptyNull();

                foreach (var valueId in form[key].ToString().SplitSafe(","))
                {
                    var attributeFilter = new ProductBundleItemAttributeFilter
                    {
                        BundleItemId = bundleItem.Id,
                        AttributeId = attributeId,
                        AttributeValueId = valueId.ToInt(),
                        IsPreSelected = (preSelectId == valueId)
                    };

                    _db.ProductBundleItemAttributeFilter.Add(attributeFilter);
                }

                await _db.SaveChangesAsync();
            }
        }

        #endregion

        #region Update[...]

        protected async Task<bool> MapModelToProductAsync(ProductModel model, Product product, IFormCollection form)
        {
            if (model.LoadedTabs == null || model.LoadedTabs.Length == 0)
            {
                model.LoadedTabs = new string[] { "Info" };
            }

            // TODO: (mh) (core) API-Design: please describe the purpose of the return value.
            var nameChanged = false;

            foreach (var tab in model.LoadedTabs)
            {
                switch (tab.ToLowerInvariant())
                {
                    case "info":
                        UpdateProductGeneralInfo(product, model, out nameChanged);
                        break;
                    case "inventory":
                        await UpdateProductInventoryAsync(product, model);
                        break;
                    case "bundleitems":
                        await UpdateProductBundleItemsAsync(product, model);
                        break;
                    case "price":
                        await UpdateProductPriceAsync(product, model);
                        break;
                    case "attributes":
                        UpdateProductAttributes(product, model);
                        break;
                    case "downloads":
                        await UpdateProductDownloadsAsync(product, model);
                        break;
                    case "pictures":
                        UpdateProductPictures(product, model);
                        break;
                    case "seo":
                        await UpdateProductSeoAsync(product, model);
                        break;
                }
            }

            await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, product, form));

            return nameChanged;
        }

        protected void UpdateProductGeneralInfo(Product product, ProductModel model, out bool nameChanged)
        {
            var p = product;
            var m = model;

            p.ProductTypeId = m.ProductTypeId;
            p.Visibility = m.Visibility;
            p.Condition = m.Condition;
            p.ProductTemplateId = m.ProductTemplateId;

            nameChanged = !string.Equals(p.Name, m.Name, StringComparison.CurrentCultureIgnoreCase);

            p.Name = m.Name;
            p.ShortDescription = m.ShortDescription;
            p.FullDescription = m.FullDescription;
            p.Sku = m.Sku;
            p.ManufacturerPartNumber = m.ManufacturerPartNumber;
            p.Gtin = m.Gtin;
            p.AdminComment = m.AdminComment;
            p.AvailableStartDateTimeUtc = m.AvailableStartDateTimeUtc;
            p.AvailableEndDateTimeUtc = m.AvailableEndDateTimeUtc;

            p.AllowCustomerReviews = m.AllowCustomerReviews;
            p.ShowOnHomePage = m.ShowOnHomePage;
            p.HomePageDisplayOrder = m.HomePageDisplayOrder;
            p.Published = m.Published;
            p.RequireOtherProducts = m.RequireOtherProducts;
            p.RequiredProductIds = m.RequiredProductIds;
            p.AutomaticallyAddRequiredProducts = m.AutomaticallyAddRequiredProducts;

            p.IsGiftCard = m.IsGiftCard;
            p.GiftCardTypeId = m.GiftCardTypeId;

            p.IsRecurring = m.IsRecurring;
            p.RecurringCycleLength = m.RecurringCycleLength;
            p.RecurringCyclePeriodId = m.RecurringCyclePeriodId;
            p.RecurringTotalCycles = m.RecurringTotalCycles;

            p.IsShippingEnabled = m.IsShippingEnabled;
            p.DeliveryTimeId = m.DeliveryTimeId == 0 ? null : m.DeliveryTimeId;
            p.QuantityUnitId = m.QuantityUnitId == 0 ? null : m.QuantityUnitId;
            p.IsFreeShipping = m.IsFreeShipping;
            p.AdditionalShippingCharge = m.AdditionalShippingCharge ?? 0;
            p.Weight = m.Weight ?? 0;
            p.Length = m.Length ?? 0;
            p.Width = m.Width ?? 0;
            p.Height = m.Height ?? 0;

            p.IsEsd = m.IsEsd;
            p.IsTaxExempt = m.IsTaxExempt;
            p.TaxCategoryId = m.TaxCategoryId ?? 0;
            p.CustomsTariffNumber = m.CustomsTariffNumber;
            p.CountryOfOriginId = m.CountryOfOriginId == 0 ? null : m.CountryOfOriginId;

            p.AvailableEndDateTimeUtc = p.AvailableEndDateTimeUtc.ToEndOfTheDay();
            p.SpecialPriceEndDateTimeUtc = p.SpecialPriceEndDateTimeUtc.ToEndOfTheDay();
        }

        protected async Task UpdateProductDownloadsAsync(Product product, ProductModel model)
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Media.Download.Update))
            {
                return;
            }

            var p = product;
            var m = model;

            p.IsDownload = m.IsDownload;
            //p.DownloadId = m.DownloadId ?? 0;
            p.UnlimitedDownloads = m.UnlimitedDownloads;
            p.MaxNumberOfDownloads = m.MaxNumberOfDownloads;
            p.DownloadExpirationDays = m.DownloadExpirationDays;
            p.DownloadActivationTypeId = m.DownloadActivationTypeId;
            p.HasUserAgreement = m.HasUserAgreement;
            p.UserAgreementText = m.UserAgreementText;
            p.HasSampleDownload = m.HasSampleDownload;
            p.SampleDownloadId = m.SampleDownloadId == 0 ? null : m.SampleDownloadId;
        }

        protected async Task UpdateProductInventoryAsync(Product product, ProductModel model)
        {
            var p = product;
            var m = model;
            var updateStockQuantity = true;
            var stockQuantityInDatabase = product.StockQuantity;

            if (p.ManageInventoryMethod == ManageInventoryMethod.ManageStock && p.Id != 0)
            {
                if (m.OriginalStockQuantity != stockQuantityInDatabase)
                {
                    // The stock has changed since the edit page was loaded, e.g. because an order has been placed.
                    updateStockQuantity = false;

                    if (m.StockQuantity != m.OriginalStockQuantity)
                    {
                        // The merchant has changed the stock quantity manually.
                        NotifyWarning(T("Admin.Catalog.Products.StockQuantityNotChanged", stockQuantityInDatabase.ToString("N0")));
                    }
                }
            }

            if (updateStockQuantity)
            {
                p.StockQuantity = m.StockQuantity;
            }

            p.ManageInventoryMethodId = m.ManageInventoryMethodId;
            p.DisplayStockAvailability = m.DisplayStockAvailability;
            p.DisplayStockQuantity = m.DisplayStockQuantity;
            p.MinStockQuantity = m.MinStockQuantity;
            p.LowStockActivityId = m.LowStockActivityId;
            p.NotifyAdminForQuantityBelow = m.NotifyAdminForQuantityBelow;
            p.BackorderModeId = m.BackorderModeId;
            p.AllowBackInStockSubscriptions = m.AllowBackInStockSubscriptions;
            p.OrderMinimumQuantity = m.OrderMinimumQuantity;
            p.OrderMaximumQuantity = m.OrderMaximumQuantity;
            p.QuantityStep = m.QuantityStep;
            p.HideQuantityControl = m.HideQuantityControl;

            if (p.ManageInventoryMethod == ManageInventoryMethod.ManageStock && updateStockQuantity)
            {
                // Back in stock notifications.
                if (p.BackorderMode == BackorderMode.NoBackorders &&
                    p.AllowBackInStockSubscriptions &&
                    p.StockQuantity > 0 &&
                    stockQuantityInDatabase <= 0 &&
                    p.Published &&
                    !p.Deleted &&
                    !p.IsSystemProduct)
                {
                    await _stockSubscriptionService.Value.SendNotificationsToSubscribersAsync(p);
                }

                if (p.StockQuantity != stockQuantityInDatabase)
                {
                    await _productService.AdjustInventoryAsync(p, null, true, 0);
                }
            }
        }

        protected async Task UpdateProductBundleItemsAsync(Product product, ProductModel model)
        {
            var p = product;
            var m = model;

            p.BundleTitleText = m.BundleTitleText;
            p.BundlePerItemPricing = m.BundlePerItemPricing;
            p.BundlePerItemShipping = m.BundlePerItemShipping;
            p.BundlePerItemShoppingCart = m.BundlePerItemShoppingCart;

            // SEO
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(product, x => x.BundleTitleText, localized.BundleTitleText, localized.LanguageId);
            }
        }

        protected async Task UpdateProductPriceAsync(Product product, ProductModel model)
        {
            var p = product;
            var m = model;

            p.Price = m.Price;
            p.OldPrice = m.OldPrice ?? 0;
            p.ProductCost = m.ProductCost ?? 0;
            p.SpecialPrice = m.SpecialPrice;
            p.SpecialPriceStartDateTimeUtc = m.SpecialPriceStartDateTimeUtc;
            p.SpecialPriceEndDateTimeUtc = m.SpecialPriceEndDateTimeUtc;
            p.DisableBuyButton = m.DisableBuyButton;
            p.DisableWishlistButton = m.DisableWishlistButton;
            p.AvailableForPreOrder = m.AvailableForPreOrder;
            p.CallForPrice = m.CallForPrice;
            p.CustomerEntersPrice = m.CustomerEntersPrice;
            p.MinimumCustomerEnteredPrice = m.MinimumCustomerEnteredPrice ?? 0;
            p.MaximumCustomerEnteredPrice = m.MaximumCustomerEnteredPrice ?? 0;

            p.BasePriceEnabled = m.BasePriceEnabled;
            p.BasePriceBaseAmount = m.BasePriceBaseAmount;
            p.BasePriceAmount = m.BasePriceAmount;
            p.BasePriceMeasureUnit = m.BasePriceMeasureUnit;

            // Discounts.
            await _discountService.ApplyDiscountsAsync(product, model.SelectedDiscountIds, DiscountType.AssignedToSkus);
        }

        protected void UpdateProductAttributes(Product product, ProductModel model)
        {
            product.AttributeChoiceBehaviour = model.AttributeChoiceBehaviour;
        }

        protected async Task UpdateProductSeoAsync(Product product, ProductModel model)
        {
            var p = product;
            var m = model;

            p.MetaKeywords = m.MetaKeywords;
            p.MetaDescription = m.MetaDescription;
            p.MetaTitle = m.MetaTitle;

            var service = _localizedEntityService;
            foreach (var localized in model.Locales)
            {
                await service.ApplyLocalizedValueAsync(product, x => x.MetaKeywords, localized.MetaKeywords, localized.LanguageId);
                await service.ApplyLocalizedValueAsync(product, x => x.MetaDescription, localized.MetaDescription, localized.LanguageId);
                await service.ApplyLocalizedValueAsync(product, x => x.MetaTitle, localized.MetaTitle, localized.LanguageId);
            }
        }

        protected void UpdateProductPictures(Product product, ProductModel model)
        {
            product.HasPreviewPicture = model.HasPreviewPicture;
        }

        private async Task UpdateDataOfExistingProductAsync(Product product, ProductModel model, bool editMode, bool nameChanged)
        {
            var p = product;
            var m = model;

            //var seoTabLoaded = m.LoadedTabs.Contains("SEO", StringComparer.OrdinalIgnoreCase);

            // SEO.
            var validateSlugResult = await p.ValidateSlugAsync(p.Name, true, 0);
            m.SeName = validateSlugResult.Slug;
            await _urlService.ApplySlugAsync(validateSlugResult);

            if (editMode)
            {
                _db.Products.Update(p);
                await _db.SaveChangesAsync();
            }

            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(product, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(product, x => x.ShortDescription, localized.ShortDescription, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(product, x => x.FullDescription, localized.FullDescription, localized.LanguageId);

                validateSlugResult = await p.ValidateSlugAsync(localized.Name, false, localized.LanguageId);
                await _urlService.ApplySlugAsync(validateSlugResult);
            }

            await _productTagService.UpdateProductTagsAsync(p, m.ProductTags);

            await SaveStoreMappingsAsync(p, model.SelectedStoreIds);
            await SaveAclMappingsAsync(p, model.SelectedCustomerRoleIds);
        }

        #endregion
    }
}
