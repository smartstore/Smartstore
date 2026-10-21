namespace Smartstore.Web.Models.Media;

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
    /// Offers whole-icon opacity.
    /// </summary>
    Opacity = 64,

    /// <summary>
    /// Offers all supported presentation fields.
    /// </summary>
    All = Size | Animation | Flip | Rotate | StrokeScale | Color | Opacity
}
