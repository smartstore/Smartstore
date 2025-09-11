# Menus

## Overview
Smartstore's menu system builds navigation trees for frontend and backend. Menus consist of hierarchical `MenuItem` nodes resolved to final URLs. Items can originate from the database or be generated in code through menu providers.

## Loading and rendering
Use `IMenuService` to retrieve a menu and feed it to the built-in view component or tag helper:

```csharp
public class HeaderViewComponent : ViewComponent
{
    private readonly IMenuService _menuService;

    public HeaderViewComponent(IMenuService menuService)
    {
        _menuService = menuService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var menu = await _menuService.GetMenuAsync("Main");
        return View(await menu.CreateModelAsync());
    }
}
```

In Razor you can also render a menu directly:

```razor
@await Component.InvokeAsync("Menu", new { name = "Main" })
```

## Building menus in code
Modules can contribute items by implementing `IMenuProvider`. The following example appends an entry to the Admin menu:

```csharp
using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Builders;

public class AdminMenu : AdminMenuProvider
{
    protected override void BuildMenuCore(TreeNode<MenuItem> modulesNode)
    {
        var item = new MenuItem().ToBuilder()
            .Text("Demo")
            .Icon("rocket", "fas")
            .Action("Index", "DemoAdmin", new { area = "Admin" })
            .AsItem();

        modulesNode.Append(item);
    }
}
```

Instead of pointing to an MVC action you can also supply a link expression with `.Url("product:123")` or `.Url("https://example.com")`. The [link resolver](../advanced/linkresolver.md) translates these expressions when the menu renders.

`IMenuItemProvider` is available for generating dynamic children, such as categories. Providers are discovered through dependency injection and run during menu building. The `MenuBuiltEvent` fires after all providers execute, allowing last-minute adjustments.

## Link resolution
Menu items store their target as link expressions, which the [link resolver](../advanced/linkresolver.md) translates to absolute URLs using `ILinkProvider` implementations. Built-in providers handle MVC routes, entity links, and external URLs. See the guide for expression syntax and available schemas.

## User-defined menus
Administrators can create menus under **Content > Menus**. The link picker stores targets as expressions consumed by the [link resolver](../advanced/linkresolver.md). These menus are persisted in the database and rendered via the `Menu` view component. Widgets may embed such menus into arbitrary zones.