using Smartstore.Core.Content.Menus;
using Smartstore.Core.OutputCache;
using Smartstore.Web.Rendering.Menus;

namespace Smartstore.Web.Components
{
    public class MenuViewComponent : SmartViewComponent
    {
        private readonly IMenuService _menuService;

        public MenuViewComponent(IMenuService menuService)
        {
            _menuService = menuService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string name, string template = null)
        {
            Guard.NotEmpty(name);

            var menu = await _menuService.GetMenuAsync(name);

            if (menu == null)
            {
                return Empty();
            }

            var model = await menu.CreateModelAsync(template, ViewContext);
            
            var viewName = model.Template ?? model.Name;
            if (viewName[0] != '~' && !viewName.StartsWith("Menus/", StringComparison.OrdinalIgnoreCase))
            {
                //viewName = "Menus/" + viewName;
                //viewName = "~/Views/Shared/Menus/" + viewName + ".cshtml";
            }

            // Get menu entity id from model and announce entity to display control.
            if (model.Root.Value.MenuId > 0)
            {
                Services.DisplayControl.Announce(new MenuEntity { Id = model.Root.Value.MenuId });
            }

            return View(viewName, model);
        }
    }
}
