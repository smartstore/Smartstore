using System.Linq.Dynamic.Core;
using Smartstore.Admin.Models.Catalog;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Logging;
using Smartstore.Core.Security;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public partial class ProductController : AdminController
    {
        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductList(GridCommand command, ProductListModel model)
        {
            var searchQuery = CreateSearchQuery(command, model, _searchSettings.UseCatalogSearchInBackend);
            IPagedList<Product> products;

            if (_searchSettings.UseCatalogSearchInBackend)
            {
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

            TempData["OriginalStockQuantities"] = products
                .Select(x => new { x.Id, x.StockQuantity })
                .ToDictionary(x => x.Id, x => x.StockQuantity);

            var rows = await products.MapAsync(Services.MediaService);

            return Json(new GridModel<ProductOverviewModel>
            {
                Rows = rows,
                Total = products.TotalCount
            });
        }

        private CatalogSearchQuery CreateSearchQuery(GridCommand command, ProductListModel model, bool useCatalogSearch)
        {
            var fields = new List<string> { "name" };
            if (_searchSettings.SearchFields.Contains("sku"))
            {
                fields.Add("sku");
            }

            if (_searchSettings.SearchFields.Contains("shortdescription"))
            {
                fields.Add("shortdescription");
            }

            var query = new CatalogSearchQuery(fields.ToArray(), model.SearchProductName)
                .HasStoreId(model.SearchStoreId)
                .WithCurrency(_workContext.WorkingCurrency)
                .WithLanguage(_workContext.WorkingLanguage);

            if (model.SearchIsPublished.HasValue)
            {
                query = query.PublishedOnly(model.SearchIsPublished.Value);
            }

            if (model.SearchHomePageProducts.HasValue)
            {
                query = query.HomePageProductsOnly(model.SearchHomePageProducts.Value);
            }

            if (model.SearchProductTypeId > 0)
            {
                query = query.IsProductType((ProductType)model.SearchProductTypeId);
            }

            if (model.SearchWithoutManufacturers.HasValue)
            {
                query = query.HasAnyManufacturer(!model.SearchWithoutManufacturers.Value);
            }
            else if (model.SearchManufacturerId != 0)
            {
                query = query.WithManufacturerIds(null, model.SearchManufacturerId);
            }

            if (model.SearchWithoutCategories.HasValue)
            {
                query = query.HasAnyCategory(!model.SearchWithoutCategories.Value);
            }
            else if (model.SearchCategoryId != 0)
            {
                query = query.WithCategoryIds(null, model.SearchCategoryId);
            }

            if (!model.SearchDeliveryTimeIds.IsNullOrEmpty())
            {
                query = query.WithDeliveryTimeIds(model.SearchDeliveryTimeIds);
            }

            query.ParseSearchTerm = !useCatalogSearch;

            if (useCatalogSearch)
            {
                query = query.Slice((command.Page - 1) * command.PageSize, command.PageSize);

                var sort = command.Sorting?.FirstOrDefault();
                if (sort != null)
                {
                    switch (sort.Member)
                    {
                        case nameof(ProductModel.Name):
                            query = query.SortBy(sort.Descending ? ProductSortingEnum.NameDesc : ProductSortingEnum.NameAsc);
                            break;
                        case nameof(ProductModel.Price):
                            query = query.SortBy(sort.Descending ? ProductSortingEnum.PriceDesc : ProductSortingEnum.PriceAsc);
                            break;
                        case nameof(ProductModel.CreatedOn):
                            query = query.SortBy(sort.Descending ? ProductSortingEnum.CreatedOn : ProductSortingEnum.CreatedOnAsc);
                            break;
                    }
                }

                if (query.Sorting.Count == 0)
                {
                    query = query.SortBy(ProductSortingEnum.NameAsc);
                }
            }

            return query;
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Delete)]
        public async Task<IActionResult> ProductDelete(GridSelection selection)
        {
            var entities = await _db.Products.GetManyAsync(selection.GetEntityIds(), true);
            if (entities.Count > 0)
            {
                _db.Products.RemoveRange(entities);
                await _db.SaveChangesAsync();

                Services.ActivityLogger.LogActivity(
                    KnownActivityLogTypes.DeleteProduct, 
                    T("ActivityLog.DeleteProduct"),
                    string.Join(", ", entities.Select(x => x.Name)));
            }

            return Json(new { Success = true, entities.Count });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Update)]
        public async Task<IActionResult> ProductUpdate(ProductOverviewModel model)
        {
            var product = await _db.Products.FindByIdAsync(model.Id);
            if (product == null)
            {
                NotifyError(T("Admin.Common.ResourceNotFound"));
                return Json(new { success = false });
            }

            try
            {
                // INFO: SmartTempDataSerializer already handles proper deserialization. No need for conversion here.
                if (TempData.TryGetValueAs<Dictionary<int, int>>("OriginalStockQuantities", out var originalStockQuantities)
                    && originalStockQuantities.TryGetValue(product.Id, out var originalStockQuantity)
                    && CheckStockQuantityUpdate(product, originalStockQuantity, model.StockQuantity))
                {
                    product.StockQuantity = model.StockQuantity;
                }

                product.Name = model.Name;
                product.Sku = model.Sku;
                product.Price = model.Price;
                product.Published = model.Published;
                product.ManufacturerPartNumber = model.ManufacturerPartNumber;
                product.Gtin = model.Gtin;
                product.MinStockQuantity = model.MinStockQuantity;
                product.ComparePrice = model.ComparePrice ?? 0;
                product.AvailableStartDateTimeUtc = model.AvailableStartDateTimeUtc;
                product.AvailableEndDateTimeUtc = model.AvailableEndDateTimeUtc;
                product.DeliveryTimeId = model.DeliveryTimeId;
                product.DisplayOrder = model.DisplayOrder;

                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                NotifyError(ex.GetInnerMessage());
                return Json(new { success = false });
            }
        }
    }
}
