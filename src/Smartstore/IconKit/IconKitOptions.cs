namespace Smartstore.IconKit;

/// <summary>
/// Configures icon sources, generated artifacts and HTTP routes.
/// Relative file paths are resolved against the host's content root.
/// </summary>
public class IconKitOptions
{
    /// <summary>
    /// Gets or sets the directory containing config.json, kit definitions and libraries.
    /// </summary>
    public string SourcePath { get; set; } = "App_Data/Icons";

    /// <summary>
    /// Gets or sets the writable directory containing generated kit and browser artifacts.
    /// </summary>
    public string CachePath { get; set; } = "App_Data/.cache/icons";

    /// <summary>
    /// Gets or sets the application-relative route prefix, without the request path base.
    /// </summary>
    public string RoutePrefix { get; set; } = "/icons";
}
