using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Smartstore.Core.Content.Media.Icons;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Media;

/// <summary>
/// Edits a persistent icon address and optional presentation overrides.
/// Null option values preserve address and mapping defaults.
/// </summary>
public class IconModel : ModelBase
{
    /// <summary>
    /// Gets or sets the original address, including optional library, variant and query modifiers.
    /// </summary>
    public string IconAddress { get; set; }

    /// <summary>
    /// Gets or sets a predefined icon size.
    /// </summary>
    public string Size { get; set; }

    /// <summary>
    /// Gets or sets an icon animation.
    /// </summary>
    public string Animation { get; set; }

    /// <summary>
    /// Gets or sets horizontal mirroring. Null inherits the address or mapping value.
    /// </summary>
    public bool? FlipHorizontal { get; set; }

    /// <summary>
    /// Gets or sets vertical mirroring. Null inherits the address or mapping value.
    /// </summary>
    public bool? FlipVertical { get; set; }

    /// <summary>
    /// Gets or sets clockwise rotation in degrees. Zero explicitly resets inherited rotation.
    /// </summary>
    public int? Rotate { get; set; }

    /// <summary>
    /// Gets or sets a positive stroke multiplier. Null inherits the address or mapping value.
    /// </summary>
    public double? StrokeScale { get; set; }

    /// <summary>
    /// Gets or sets editor configuration, independently of persisted presentation values.
    /// None hides the options button. Controllers must restore this configuration on redisplay.
    /// </summary>
    [BindNever, JsonIgnore]
    public IconOptionFields VisibleOptions { get; set; }

    /// <summary>
    /// Creates rendering options without changing the persisted address or omitted values.
    /// </summary>
    public IconOptions ToOptions() => new()
    {
        Size = Size,
        Animation = Animation,
        FlipHorizontal = FlipHorizontal,
        FlipVertical = FlipVertical,
        Rotate = Rotate,
        StrokeScale = StrokeScale
    };
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
    /// Offers all supported presentation fields.
    /// </summary>
    All = Size | Animation | Flip | Rotate | StrokeScale
}
