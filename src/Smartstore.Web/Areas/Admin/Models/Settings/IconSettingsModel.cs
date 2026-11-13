#nullable enable

using System.ComponentModel.DataAnnotations;
using Smartstore.Web.Models.Media;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Settings;

/// <summary>
/// Edits the icon defaults for Core menu areas.
/// </summary>
[LocalizedDisplay("Admin.Configuration.Settings.Icons.Fields.")]
public class IconSettingsModel : ModelBase
{
    /// <summary>
    /// Gets or sets the main menu icon options.
    /// </summary>
    [LocalizedDisplay("*MainMenu")]
    public MenuIconOptionsModel MainMenu { get; set; } = new();

    /// <summary>
    /// Gets or sets the main menu dropdown icon options.
    /// </summary>
    [LocalizedDisplay("*MainMenuDropdown")]
    public MenuIconOptionsModel MainMenuDropdown { get; set; } = new();

    /// <summary>
    /// Gets or sets the off-canvas menu icon options.
    /// </summary>
    [LocalizedDisplay("*OffCanvasMenu")]
    public MenuIconOptionsModel OffCanvasMenu { get; set; } = new();

    /// <summary>
    /// Gets or sets the category menu icon options.
    /// </summary>
    [LocalizedDisplay("*CategoryMenu")]
    public MenuIconOptionsModel CategoryMenu { get; set; } = new();

    /// <summary>
    /// Gets or sets the icon size preset for the My Account menu.
    /// </summary>
    [LocalizedDisplay("*MyAccountIconSize")]
    public string MyAccountIconSize { get; set; } = "md";
}

/// <summary>
/// Edits the icon defaults saved together for one menu area.
/// </summary>
[LocalizedDisplay("Admin.Configuration.Settings.Icons.Fields.")]
public class MenuIconOptionsModel : ModelBase
{
    /// <summary>
    /// Gets or sets whether entry icons are displayed.
    /// </summary>
    [LocalizedDisplay("*DisplayIcons")]
    public bool DisplayIcons { get; set; } = true;

    /// <summary>
    /// Gets or sets the menu area's size preset.
    /// </summary>
    [LocalizedDisplay("*Size")]
    public string Size { get; set; } = "md";

    /// <summary>
    /// Gets or sets the fallback icon selection.
    /// </summary>
    [LocalizedDisplay("*FallbackIcon"), UIHint("IconBrowser")]
    public IconBrowserModel FallbackIcon { get; set; } = new()
    {
        Area = "public",
        VisibleOptions = IconOptionFields.All & ~IconOptionFields.Size
    };
}