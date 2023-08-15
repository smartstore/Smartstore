using Smartstore.Admin.Models.Catalog;
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
    }
}
