using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Web.Controllers;

namespace Smartstore.Web.Components
{
    public class BreadcrumbViewComponent : SmartViewComponent
    {
        // TODO: (mh) (core) Don't forget to remove SmartDbContext & IMenuService once testcode for GetBreadcrumbAsync is gone.
        private readonly SmartDbContext _db;            
        private readonly IMenuService _menuService;
        private readonly IBreadcrumb _breadcrumb;

        public BreadcrumbViewComponent(SmartDbContext db, IMenuService menuService, IBreadcrumb breadcrumb)
        {
            _db = db;
            _menuService = menuService;
            _breadcrumb = breadcrumb;
        }

        public IViewComponentResult Invoke(IEnumerable<MenuItem> trail = null)
        {
            // TODO: (mh) (core) Remove test code when helper method is implemented and gets invoked.
            //await CreateProductTestCase();

            trail ??= _breadcrumb.Trail;

            if (trail == null || !trail.Any())
            {
                return Empty();
            }

            return View(trail);
        }

        // TODO: (mh) (core) Remove test code.
        #region Test code

        private async Task CreateProductTestCase()
        {
            var product = await _db.Products.FindByIdAsync(4);

            await GetBreadcrumbAsync(_breadcrumb, ViewContext, product);

            _breadcrumb.Track(new MenuItem
            {
                Text = product.GetLocalized(x => x.Name),
                Rtl = false,
                EntityId = product.Id,
                Url = await product.GetActiveSlugAsync()
            });
        }

        // TODO: (mh) (core) Remove or move this to helper class.
        private async Task GetBreadcrumbAsync(IBreadcrumb breadcrumb, ViewContext context, Product product = null)
        {
            var menu = await _menuService.GetMenuAsync("Main");

            // TODO: (mh) (core) Remove test code.
            // BEGIN: Manipulate Context to see some test results
            //context.RouteData.Values.Add("currentCategoryId", 1794);
            context.RouteData.Values.Add("currentProductId", product != null ? product.Id : 0);
            // END: Manipulate Context 

            var currentNode = await menu.ResolveCurrentNodeAsync(context);

            if (currentNode != null)
            {
                currentNode.Trail.Where(x => !x.IsRoot).Each(x => breadcrumb.Track(x.Value));
            }

            // Add trail of parent product if product has no category assigned.
            if (product != null && !(breadcrumb.Trail?.Any() ?? false))
            {
                var parentProduct = await _db.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == product.ParentGroupedProductId);

                if (parentProduct != null)
                {
                    var fc = new PublicControllerBase();    // TODO: (mh) (core) PublicControllerBase was FakeController.
                    var rd = new RouteData();
                    rd.Values.Add("currentProductId", parentProduct.Id);
                    var fcc = new ControllerContext(new ActionContext(context.HttpContext, rd, context.ActionDescriptor));
                    fc.ControllerContext = fcc;

                    currentNode = await menu.ResolveCurrentNodeAsync(fcc);
                    if (currentNode != null)
                    {
                        currentNode.Trail.Where(x => !x.IsRoot).Each(x => breadcrumb.Track(x.Value));
                        var parentName = parentProduct.GetLocalized(x => x.Name);

                        breadcrumb.Track(new MenuItem
                        {
                            Text = parentName,
                            Rtl = parentName.CurrentLanguage.Rtl,
                            EntityId = parentProduct.Id,
                            Url = await parentProduct.GetActiveSlugAsync()
                        });
                    }
                }
            }
        }

        #endregion
    }
}
