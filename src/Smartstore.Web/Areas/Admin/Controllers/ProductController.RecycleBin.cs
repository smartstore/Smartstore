using Smartstore.Admin.Models.Catalog;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
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

            rows.Each(x => x.EditUrl = "javascript:;");

            return Json(new GridModel<ProductOverviewModel>
            {
                Rows = rows,
                Total = products.TotalCount
            });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Delete)]
        public async Task<IActionResult> DeletedProductsDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                $"- delete {string.Join(",", ids)}".Dump();
                await Task.Delay(10);
                // TODO: (mg)...
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Create)]
        public async Task<IActionResult> DeletedProductsRestore(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numRestored = 0;

            if (ids.Any())
            {
                $"- restore {string.Join(",", ids)}".Dump();
                await Task.Delay(10);
                // TODO: (mg)...
            }

            return Json(new { Success = true, Count = numRestored });
        }

        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> DeletedProductDetails(int id)
        {
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
            model.ProductTagNames = product.ProductTags.Select(x => x.GetLocalized(y => y.Name).Value).ToArray();
            model.PictureThumbnailUrl = _mediaService.GetUrl(await _mediaService.GetFileByIdAsync(product.MainPictureId ?? 0), _mediaSettings.CartThumbPictureSize);

            ViewBag.Price = new Money(product.Price, Services.CurrencyService.PrimaryCurrency);

            ViewBag.NumberOfOrders = await _db.Orders
                .Where(x => x.OrderItems.Any(oi => oi.ProductId == id))
                .Select(x => x.Id)
                .Distinct()
                .CountAsync();

            var manufacturers = await _db.ProductManufacturers
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(x => x.Manufacturer)
                .Where(x => x.ProductId == id)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => x.Manufacturer)
                .ToListAsync();

            ViewBag.Manufacturers = manufacturers
                .Select(x => x.GetLocalized(y => y.Name).Value)
                .Where(x => x.HasValue())
                .ToDistinctArray(x => x);

            var categories = await _db.ProductCategories
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(x => x.Category)
                .Where(x => x.ProductId == id)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => x.Category)
                .ToListAsync();

            ViewBag.Categories = categories
                .Select(x => x.GetLocalized(y => y.Name).Value)
                .Where(x => x.HasValue())
                .ToDistinctArray(x => x);

            var productAttributes = await _db.ProductVariantAttributes
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(x => x.ProductAttribute)
                .Where(x => x.ProductId == id)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => x.ProductAttribute)
                .ToListAsync();

            ViewBag.ProductAttributes = productAttributes
                .Select(x => x.GetLocalized(y => y.Name).Value)
                .Where(x => x.HasValue())
                .ToDistinctArray(x => x);

            var specificationAttributes = await _db.ProductSpecificationAttributes
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(x => x.SpecificationAttributeOption.SpecificationAttribute)
                .Where(x => x.ProductId == id)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => x.SpecificationAttributeOption.SpecificationAttribute)
            .ToListAsync();

            ViewBag.SpecificationAttributes = specificationAttributes
                .Select(x => x.GetLocalized(y => y.Name).Value)
                .Where(x => x.HasValue())
                .ToDistinctArray(x => x);

            return View(model);
        }
    }
}
