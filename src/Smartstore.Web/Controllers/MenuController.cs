using Smartstore.Core.Catalog;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Security;
using Smartstore.Web.Rendering.Menus;

namespace Smartstore.Web.Controllers
{
    public class MenuController : SmartController
    {
        private readonly IMenuService _menuService;
        private readonly CatalogSettings _catalogSettings;
        private readonly CatalogHelper _catalogHelper;

        public MenuController(
            IMenuService menuService,
            CatalogSettings catalogSettings,
            CatalogHelper catalogHelper)
        {
            _menuService = menuService;
            _catalogSettings = catalogSettings;
            _catalogHelper = catalogHelper;
        }

        #region OffCanvasMenu 

        /// <summary>
        /// Called by AJAX to get the an OffCanvas layer (either the root "home" or a "submenu" layer)
        /// </summary>
        /// <param name="currentNodeId">Id of currently selected node/page from sm:pagedata meta tag</param>
        /// <param name="targetNodeId">Id of the parent node to which should be navigated in the OffCanvasMenu (actually the node which was clicked)</param>
        [HttpPost]
        public async Task<IActionResult> OffCanvas(string currentNodeId, string targetNodeId)
        {
            bool allowNavigation = await Services.Permissions.AuthorizeAsync(Permissions.System.AccessShop);

            ViewBag.AllowNavigation = allowNavigation;
            ViewBag.ShowNodes = allowNavigation;
            ViewBag.ShowBrands = allowNavigation
                && _catalogSettings.ShowManufacturersInOffCanvas == true
                && _catalogSettings.ManufacturerItemsToDisplayInOffcanvasMenu > 0;

            if (!allowNavigation)
            {
                return View("OffCanvas.Home", null);
            }

            var model = await PrepareMenuModelAsync(currentNodeId, targetNodeId);
            if (model == null)
            {
                return new EmptyResult();
            }

            var selectedNode = model.SelectedNode;
            var isHomeLayer = selectedNode.IsRoot || (selectedNode.Depth == 1 && selectedNode.IsLeaf);

            var templateName = isHomeLayer
                // Render home layer, if parent node is either home or a direct child of home
                ? "OffCanvas.Home"
                // Render a submenu
                : "OffCanvas.Menu";

            return PartialView(templateName, model);
        }

        [HttpPost]
        public async Task<IActionResult> OffCanvasBrands()
        {
            var model = await _catalogHelper.PrepareBrandNavigationModelAsync(_catalogSettings.ManufacturerItemsToDisplayInOffcanvasMenu);
            return PartialView("OffCanvas.Brands", model);
        }

        /// <summary>
        /// Prepares the menu for given parent node 'targetNodeId' with current node 'currentNodeId'
        /// </summary>
        /// <param name="currentNodeId">Id of currently selected node/page from sm:pagedata meta tag.</param>
        /// <param name="targetNodeId">Id of the parent node to which should be navigated in the OffCanvasMenu (actually the node which was clicked).</param>
        protected async Task<MenuModel> PrepareMenuModelAsync(string currentNodeId, string targetNodeId)
        {
            var menu = await _menuService.GetMenuAsync("Main");
            if (menu == null)
            {
                return null;
            }

            object nodeId = ConvertNodeId(targetNodeId);
            var root = await menu.GetRootNodeAsync();

            var model = new MenuModel
            {
                Name = "offcanvas",
                Root = root,
                SelectedNode = IsNullNode(nodeId)
                    ? root
                    : root.SelectNodeById(nodeId)
            };

            await menu.ResolveElementCountAsync(model.SelectedNode, false);

            if (currentNodeId == targetNodeId)
            {
                ViewBag.CurrentNode = model.SelectedNode;
            }
            else
            {
                nodeId = ConvertNodeId(currentNodeId);
                if (!IsNullNode(nodeId))
                {
                    ViewBag.CurrentNode = root.SelectNodeById(nodeId);
                }
            }

            return model;
        }

        private static object ConvertNodeId(string source)
        {
            int? intId = int.TryParse(source, out var id) ? id : null;
            return intId.HasValue ? intId.Value : source;
        }

        private static bool IsNullNode(object source)
        {
            return source == null || Equals(0, source) || Equals(string.Empty, source);
        }

        #endregion
    }
}
