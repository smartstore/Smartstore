using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Smartstore.IconKit;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Media;

/// <summary>
/// Edits a persistent icon address and optional presentation overrides.
/// Null option values preserve address and mapping defaults.
/// </summary>
public class IconBrowserModel : ModelBase
{
    /// <summary>
    /// Gets or sets the selected icon and all presentation modifiers.
    /// Null represents an empty selection.
    /// </summary>
    public IconExpression? Expression { get; set; }

    /// <summary>
    /// Gets or sets the icon resolution area. Defaults to the current request area.
    /// </summary>
    [BindNever, JsonIgnore]
    public string Area { get; set; }

    /// <summary>
    /// Gets or sets editor configuration, independently of persisted presentation values.
    /// None hides the options button. Controllers must restore this configuration on redisplay.
    /// </summary>
    [BindNever, JsonIgnore]
    public IconOptionFields VisibleOptions { get; set; }

    /// <summary>
    /// Gets or sets whether the picker lists kits hidden by their default configuration.
    /// Controllers must restore this editor configuration on redisplay.
    /// </summary>
    [BindNever, JsonIgnore]
    public bool ShowHiddenKits { get; set; }

    /// <summary>
    /// Gets or sets whether provider-dependent concepts display a developer indicator and tooltip.
    /// Disabled by default. Controllers must restore this editor configuration on redisplay.
    /// </summary>
    [BindNever, JsonIgnore]
    public bool ShowProviderIndicators { get; set; }
}

/// <summary>
/// Selects presentation fields offered by the icon editor, not the values to persist.
/// </summary>
[Flags]
public enum IconOptionFields
{
    /// <summary>
    /// Hides the options editor.
    /// </summary>
    None = 0,

    /// <summary>
    /// Offers predefined sizing.
    /// </summary>
    Size = 1,

    /// <summary>
    /// Offers animation selection.
    /// </summary>
    Animation = 2,

    /// <summary>
    /// Offers horizontal and vertical mirroring.
    /// </summary>
    Flip = 4,

    /// <summary>
    /// Offers rotation.
    /// </summary>
    Rotate = 8,

    /// <summary>
    /// Offers a stroke multiplier.
    /// </summary>
    StrokeScale = 16,

    /// <summary>
    /// Offers rendering color selection.
    /// </summary>
    Color = 32,

    /// <summary>
    /// Offers all supported presentation fields.
    /// </summary>
    All = Size | Animation | Flip | Rotate | StrokeScale | Color
}
