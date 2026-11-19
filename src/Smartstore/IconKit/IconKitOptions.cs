namespace Smartstore.IconKit;

/// <summary>
/// Configures icon sources, generated artifacts and HTTP routes.
/// Folder paths must be relative to the application's content root.
/// </summary>
public class IconKitOptions
{
    /// <summary>
    /// Gets or sets the relative folder containing config.json, kit definitions and libraries.
    /// </summary>
    public string SourcePath { get; set; } = "App_Data/Icons";

    /// <summary>
    /// Gets or sets the relative folder for generated kit and browser files.
    /// </summary>
    public string CachePath { get; set; } = "App_Data/.cache/icons";

    /// <summary>
    /// Gets or sets the application-relative route prefix, without the request path base.
    /// </summary>
    public string RoutePrefix { get; set; } = "/icons";
}
