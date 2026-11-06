#nullable enable

using Smartstore.Core.Content.Menus;
using Smartstore.IconKit;

namespace Smartstore.Web.Rendering.Menus;

/// <summary>
/// Applies menu icon defaults without changing menu items or settings.
/// </summary>
public static class MenuModelExtensions
{
    /// <summary>
    /// Gets the effective item icon, preserving explicit presentation and the legacy behavior when options are absent.
    /// </summary>
    /// <param name="model">The menu render context.</param>
    /// <param name="item">The menu item to render.</param>
    /// <param name="supportsImages">Whether the template renders menu images in place of missing icons.</param>
    /// <param name="allowFallback">Whether the current block has an entry with its own icon and allows fallback icons.</param>
    public static string? GetIcon(this MenuModel model, MenuItem item, bool supportsImages = true, bool allowFallback = false)
    {
        Guard.NotNull(model);
        Guard.NotNull(item);

        var options = model.IconOptions;
        if (options == null)
        {
            return item.Icon;
        }

        if (!options.DisplayIcons)
        {
            return null;
        }

        var icon = item.Icon.HasValue()
            ? item.Icon
            : allowFallback && !(supportsImages && item.ImageUrl.HasValue()) ? options.FallbackIcon : null;

        var size = model.GetIconSize();
        if (size == null
            || !IconExpression.TryParse(icon, out var expression)
            || expression.Presentation.Size != null
            || expression.Presentation.FontScale.HasValue)
        {
            return icon;
        }

        return (expression with
        {
            Presentation = expression.Presentation.WithDefaults(new IconPresentation { Size = size })
        }).ToString();
    }

    /// <summary>
    /// Gets the normalized area size for icons and their empty slots. Standard size uses no modifier.
    /// </summary>
    public static string? GetIconSize(this MenuModel model)
    {
        Guard.NotNull(model);

        return model.IconOptions is { DisplayIcons: true, Size: "lg" or "xl" } options ? options.Size : null;
    }
}