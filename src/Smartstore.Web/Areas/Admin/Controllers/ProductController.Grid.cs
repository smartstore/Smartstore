using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Catalog;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public partial class ProductController : AdminControllerBase
    {
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
        [Permission(Permissions.Catalog.Product.Update)]
        public async Task<IActionResult> ProductUpdate(ProductOverviewModel model)
        {
            var product = await _db.Products.FindByIdAsync(model.Id);

            product.Name = model.Name;
            product.Sku = model.Sku;
            product.Price = model.Price;
            product.StockQuantity = model.StockQuantity;
            product.Published = model.Published;
            product.ManufacturerPartNumber = model.ManufacturerPartNumber;
            product.Gtin = model.Gtin;
            product.MinStockQuantity = model.MinStockQuantity;
            product.OldPrice = model.OldPrice ?? 0;
            product.AvailableStartDateTimeUtc = model.AvailableStartDateTimeUtc;
            product.AvailableEndDateTimeUtc = model.AvailableEndDateTimeUtc;

            try
            {
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
