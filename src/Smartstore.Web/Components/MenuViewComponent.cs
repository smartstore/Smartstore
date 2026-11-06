using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Menus;

namespace Smartstore.Web.Components;

public class MenuViewComponent : SmartViewComponent
{
    private readonly IMenuService _menuService;
    private readonly IconSettings _iconSettings;

    public MenuViewComponent(IMenuService menuService, IconSettings iconSettings)
    {
        _menuService = menuService;
        _iconSettings = iconSettings;
    }

    public async Task<IViewComponentResult> InvokeAsync(string name, string template = null, string publicName = "")
    {
        Guard.NotEmpty(name);

        var menu = await _menuService.GetMenuAsync(name);

        if (menu == null)
        {
            return Empty();
        }

        var model = await menu.CreateModelAsync(template, ViewContext);
        model.PublicName = publicName;

        if (model.Name.EqualsNoCase("Main"))
        {
            if (model.Template.EqualsNoCase("Main"))
            {
                // Shared navbar defaults also apply when a module replaces the Main view.
                // Core dropdowns retain their separate options and share the same menu tree.
                model.IconOptions = _iconSettings.MainMenu;
                ViewData["MainMenuNavbarModel"] = CreateRenderModel(model, _iconSettings.MainMenu);
                ViewData["MainMenuDropdownModel"] = CreateRenderModel(model, _iconSettings.MainMenuDropdown);
            }
            else if (model.Template.EqualsNoCase("Navbar"))
            {
                model.IconOptions = _iconSettings.MainMenu;
            }
            else if (model.Template.EqualsNoCase("Categories"))
            {
                model.IconOptions = _iconSettings.CategoryMenu;
            }
        }
        else if (model.Name.EqualsNoCase("MyAccount") && model.Template.EqualsNoCase("ListGroup"))
        {
            model.IconOptions = new MenuIconOptions
            {
                Size = _iconSettings.MyAccountIconSize
            };
        }

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

    private static MenuModel CreateRenderModel(MenuModel model, MenuIconOptions iconOptions)
    {
        return new MenuModel
        {
            Name = model.Name,
            Template = model.Template,
            PublicName = model.PublicName,
            Root = model.Root,
            Path = model.Path,
            IconOptions = iconOptions
        };
    }
}