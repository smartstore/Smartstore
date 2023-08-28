using Smartstore.Admin.Models.Catalog;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Security;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public partial class ProductController : AdminController
    {
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> RecycleBin(ProductListModel model)
        {
            await PrepareProductListModelAsync(model);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> DeletedProductsList(GridCommand command, ProductListModel model)
        {
            var searchQuery = CreateSearchQuery(command, model, false);
            var baseQuery = _db.Products
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(x => x.Deleted);

            var query = _catalogSearchService.Value
                .PrepareQuery(searchQuery, baseQuery)
                .ApplyGridCommand(command, false);

            var products = await query.ToPagedList(command).LoadAsync();
            var rows = await products.MapAsync(Services.MediaService);

            var productIds = products.ToDistinctArray(x => x.Id);
            var orderCounts = await _db.Products
                .IgnoreQueryFilters()
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new
                {
                    p.Id,
                    Count = _db.Orders
                        .IgnoreQueryFilters()
                        .Where(o => o.OrderItems.Any(oi => oi.ProductId == p.Id))
                        .Select(o => o.Id)
                        .Distinct()
                        .Count()
                })
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            foreach (var row in rows)
            {
                row.EditUrl = "javascript:;";
                row.NumberOfOrders = orderCounts.Get(row.Id);
            }

            return Json(new GridModel<ProductOverviewModel>
            {
                Rows = rows,
                Total = products.TotalCount
            });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Delete)]
        public async Task<IActionResult> EmptyRecycleBin()
        {
            var ids = await _db.Products
                .IgnoreQueryFilters()
                .Where(x => x.Deleted)
                .Select(x => x.Id)
                .ToArrayAsync();

            await DeletePermanent(ids);

            return RedirectToAction(nameof(RecycleBin));
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Delete)]
        public async Task<IActionResult> DeleteProductsPermanent(ProductRecycleBinModel model)
        {
            await DeletePermanent(model.ProductIds.ToIntArray());

            return Json(null);
        }

        private async Task DeletePermanent(int[] productIds)
        {
            var result = await _productService.DeleteProductsPermanentAsync(productIds);

            NotifyInfo(T("Admin.Catalog.Products.RecycleBin.DeleteProductsResult", 
                result.DeletedRecords.ToString("N0"), 
                productIds.Length.ToString("N0"), 
                result.SkippedRecords.ToString("N0")));

            result.Errors.Take(3).Each(x => NotifyError(x));
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Create)]
        public async Task<IActionResult> RestoreProducts(ProductRecycleBinModel model)
        {
            var ids = model.ProductIds.ToIntArray();
            var restoredRecords = await _productService.RestoreProductsAsync(ids, model.PublishAfterRestore);

            NotifyInfo(T("Admin.Catalog.Products.RecycleBin.RestoreProductsResult",
                restoredRecords.ToString("N0"),
                ids.Length.ToString("N0")));

            return Json(null);
        }

        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> DeletedProductDetails(int id)
        {
            const int maxNames = 21;

            var product = await _db.Products
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(x => x.ProductTags)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<Product, ProductModel>(product);

            model.UpdatedOn = Services.DateTimeHelper.ConvertToUserTime(product.UpdatedOnUtc, DateTimeKind.Utc);
            model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(product.CreatedOnUtc, DateTimeKind.Utc);
            model.ProductTypeName = product.GetProductTypeLabel(Services.Localization);
            model.ProductTagNames = product.ProductTags.Select(x => x.Name).ToArray();
            model.PictureThumbnailUrl = _mediaService.GetUrl(await _mediaService.GetFileByIdAsync(product.MainPictureId ?? 0), _mediaSettings.CartThumbPictureSize);

            model.NumberOfOrders = await _db.Orders
                .IgnoreQueryFilters()
                .Where(x => x.OrderItems.Any(oi => oi.ProductId == id))
                .Select(x => x.Id)
                .Distinct()
                .CountAsync();

            ViewBag.Price = new Money(product.Price, Services.CurrencyService.PrimaryCurrency);

            var manufacturers = await _db.ProductManufacturers
                .IgnoreQueryFilters()
                .Where(x => x.ProductId == id)
                .Select(x => x.Manufacturer.Name)
                .Distinct()
                .OrderBy(x => x)
                .Take(maxNames)
                .ToArrayAsync();

            var categories = await _db.ProductCategories
                .IgnoreQueryFilters()
                .Where(x => x.ProductId == id)
                .Select(x => x.Category.Name)
                .Distinct()
                .OrderBy(x => x)
                .Take(maxNames)
                .ToArrayAsync();

            var productAttributes = await _db.ProductVariantAttributes
                .IgnoreQueryFilters()
                .Where(x => x.ProductId == id)
                .Select(x => x.ProductAttribute.Name)
                .Distinct()
                .OrderBy(x => x)
                .Take(maxNames)
                .ToArrayAsync();

            var specificationAttributes = await _db.ProductSpecificationAttributes
                .IgnoreQueryFilters()
                .Where(x => x.ProductId == id)
                .Select(x => x.SpecificationAttributeOption.SpecificationAttribute.Name)
                .Distinct()
                .OrderBy(x => x)
                .Take(maxNames)
                .ToArrayAsync();

            ViewBag.Manufacturers = JoinNames(manufacturers);
            ViewBag.Categories = JoinNames(categories);
            ViewBag.ProductAttributes = JoinNames(productAttributes);
            ViewBag.SpecificationAttributes = JoinNames(specificationAttributes);

            return View(model);

            static string JoinNames(string[] names)
            {
                return names.Length == maxNames
                    ? string.Join(", ", names.Take(maxNames - 1)) + "…"
                    : string.Join(", ", names);
            }
        }
    }
}
