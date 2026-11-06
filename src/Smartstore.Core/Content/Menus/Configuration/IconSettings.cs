#nullable enable

using System.ComponentModel;
using Smartstore.ComponentModel.TypeConverters;
using Smartstore.Core.Configuration;

namespace Smartstore.Core.Content.Menus;

/// <summary>
/// Configures icons for menu areas.
/// </summary>
public class IconSettings : ISettings
{
    /// <summary>
    /// Gets or sets the icon options for the main menu.
    /// </summary>
    public MenuIconOptions MainMenu { get; set; } = new();

    /// <summary>
    /// Gets or sets the icon options for main menu dropdowns.
    /// </summary>
    public MenuIconOptions MainMenuDropdown { get; set; } = new();

    /// <summary>
    /// Gets or sets the icon options for the off-canvas menu.
    /// </summary>
    public MenuIconOptions OffCanvasMenu { get; set; } = new();

    /// <summary>
    /// Gets or sets the icon options for the category menu.
    /// </summary>
    public MenuIconOptions CategoryMenu { get; set; } = new();

    /// <summary>
    /// Gets or sets the icon size preset for the My Account menu.
    /// </summary>
    public string MyAccountIconSize { get; set; } = "md";
}

/// <summary>
/// Configures icons for a menu area.
/// </summary>
[TypeConverter(typeof(JsonTypeConverter<MenuIconOptions>))]
public class MenuIconOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether icons are displayed.
    /// </summary>
    public bool DisplayIcons { get; set; } = true;

    /// <summary>
    /// Gets or sets the size preset (md, lg, or xl). The default md uses no size modifier.
    /// </summary>
    public string Size { get; set; } = "md";

    /// <summary>
    /// Gets or sets the fallback icon for menu items without an icon.
    /// </summary>
    public string? FallbackIcon { get; set; }
}