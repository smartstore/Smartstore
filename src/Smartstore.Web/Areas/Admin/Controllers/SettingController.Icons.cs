using Microsoft.AspNetCore.Mvc;
using Smartstore.Admin.Models.Settings;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Security;
using Smartstore.IconKit;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Admin.Controllers;

public partial class SettingController
{
    /// <summary>
    /// Displays the Core menu icon settings for the active store scope.
    /// </summary>
    [Permission(Permissions.Configuration.Setting.Read)]
    [LoadSetting]
    public IActionResult IconSettings(IconSettings iconSettings)
    {
        var model = new IconSettingsModel
        {
            MainMenu = PrepareMenuIconOptionsModel(iconSettings.MainMenu),
            MainMenuDropdown = PrepareMenuIconOptionsModel(iconSettings.MainMenuDropdown),
            OffCanvasMenu = PrepareMenuIconOptionsModel(iconSettings.OffCanvasMenu),
            CategoryMenu = PrepareMenuIconOptionsModel(iconSettings.CategoryMenu),
            MyAccountIconSize = NormalizeMenuIconSize(iconSettings.MyAccountIconSize)
        };

        return View(model);
    }

    /// <summary>
    /// Saves the Core menu icon settings with one store override per menu area.
    /// </summary>
    [Permission(Permissions.Configuration.Setting.Update)]
    [HttpPost, SaveSetting, FormValueRequired("save")]
    public IActionResult IconSettings(IconSettingsModel model, IconSettings iconSettings)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Replace each group so the shallow settings clone never changes cached option objects.
        iconSettings.MainMenu = CreateMenuIconOptions(model.MainMenu);
        iconSettings.MainMenuDropdown = CreateMenuIconOptions(model.MainMenuDropdown);
        iconSettings.OffCanvasMenu = CreateMenuIconOptions(model.OffCanvasMenu);
        iconSettings.CategoryMenu = CreateMenuIconOptions(model.CategoryMenu);
        iconSettings.MyAccountIconSize = NormalizeMenuIconSize(model.MyAccountIconSize);

        NotifySuccess(T("Admin.Configuration.Updated"));
        return RedirectToAction(nameof(IconSettings));
    }

    private static MenuIconOptionsModel PrepareMenuIconOptionsModel(MenuIconOptions options)
    {
        options ??= new MenuIconOptions();
        var model = new MenuIconOptionsModel
        {
            DisplayIcons = options.DisplayIcons,
            Size = NormalizeMenuIconSize(options.Size)
        };
        model.FallbackIcon.Icon = IconExpression.TryParse(options.FallbackIcon, out var icon) ? icon : null;
        return model;
    }

    private static MenuIconOptions CreateMenuIconOptions(MenuIconOptionsModel model)
    {
        return new MenuIconOptions
        {
            DisplayIcons = model.DisplayIcons,
            Size = NormalizeMenuIconSize(model.Size),
            FallbackIcon = model.FallbackIcon.Icon?.ToString().NullEmpty()
        };
    }

    private static string NormalizeMenuIconSize(string size)
        => size is "lg" or "xl" ? size : "md";
}