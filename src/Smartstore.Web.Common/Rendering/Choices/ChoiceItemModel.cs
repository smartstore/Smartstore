using System.Globalization;
using Smartstore.Core.Common;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Rendering.Choices;

public abstract class ChoiceItemModel : EntityModelBase
{
    public string Name { get; set; }
    public string SeName { get; set; }
    public string Title { get; set; }
    public string Alias { get; set; }
    public string Color { get; set; }

    /// <summary>
    /// Gets or sets the signed price adjustment of this choice item.
    /// </summary>
    public Money? PriceAdjustment { get; set; }

    /// <summary>
    /// Gets or sets the calculated final price for this choice item.
    /// </summary>
    public Money? SwatchPrice { get; set; }
    public int QuantityInfo { get; set; }
    public bool IsPreSelected { get; set; }
    public bool IsDisabled { get; set; }
    public bool IsUnavailable { get; set; }
    public string ImageUrl { get; set; }
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Ordered secondary colors for a multicolor swatch.
    /// The primary color remains in <see cref="Color"/>.
    /// </summary>
    public List<string> AdditionalColors { get; set; } = [];

    /// <summary>
    /// Gets a value indicating whether this item has a non-transparent swatch color.
    /// </summary>
    public bool HasColor
        => Color.HasValue() && !Color.EqualsNoCase("transparent");

    /// <summary>
    /// Gets a value indicating whether this item has a swatch image.
    /// </summary>
    public bool HasImage
        => ImageUrl.HasValue();

    /// <summary>
    /// A value indicating whether this item can be represented by a color or image swatch.
    /// Items without a swatch are not eligible for the card box layout.
    /// </summary>
    public bool HasSwatch
        => HasImage || HasColor;

    /// <summary>
    /// Gets the inline CSS for a swatch color, which may be a single color or a multicolor gradient.
    /// </summary>
    /// <returns>The inline CSS string for the swatch color.</returns>
    public string GetSwatchColorCss()
    {
        if (Color.IsEmpty() || Color.EqualsNoCase("transparent"))
        {
            return null;
        }

        var colors = new List<string>(4) { Color.Trim() };

        foreach (var color in AdditionalColors ?? [])
        {
            if (colors.Count == 4)
            {
                break;
            }

            if (color.HasValue() && !color.EqualsNoCase("transparent"))
            {
                colors.Add(color.Trim());
            }
        }

        var css = $"background-color: {colors[0]};";
        if (colors.Count == 1)
        {
            return css;
        }

        var stops = new List<string>(colors.Count * 2);
        for (var i = 0; i < colors.Count; i++)
        {
            var start = (i * 100d / colors.Count).ToString("0.####", CultureInfo.InvariantCulture);
            var end = ((i + 1) * 100d / colors.Count).ToString("0.####", CultureInfo.InvariantCulture);

            stops.Add($"{colors[i]} {start}%");
            stops.Add($"{colors[i]} {end}%");
        }

        return $"{css}background-image: linear-gradient(135deg, {string.Join(", ", stops)});";
    }

    /// <summary>
    /// Gets the inline CSS for a swatch, which may include a color and/or an image.
    /// </summary>
    /// <returns>The inline CSS string for the swatch.</returns>
    public string GetSwatchStyle()
    {
        var css = GetSwatchColorCss();

        if (HasImage)
        {
            css += $"background-image: url('{ImageUrl}');";
        }

        return css;
    }

    /// <summary>
    /// Gets the price adjustment text for this item, which is a string representation
    /// of the price adjustment amount with a "+" or "-" sign.
    /// </summary>
    public string GetPriceAdjustmentText()
    {
        if (PriceAdjustment is not { Amount: not 0 } priceAdjustment)
        {
            return null;
        }

        var sign = priceAdjustment.Amount > 0 ? "+" : "-";
        return sign + priceAdjustment.WithAmount(Math.Abs(priceAdjustment.Amount));
    }

    /// <summary>
    /// Gets the formatted price to display for the specified swatch price mode.
    /// </summary>
    /// <param name="displayMode">The price display mode.</param>
    /// <returns>The formatted price, or <c>null</c> if no price should or can be displayed.</returns>
    public string GetSwatchPriceText(SwatchPriceDisplayMode displayMode)
        => displayMode switch
        {
            SwatchPriceDisplayMode.None => null,
            SwatchPriceDisplayMode.Adjustment => GetPriceAdjustmentText(),
            SwatchPriceDisplayMode.FinalPrice => SwatchPrice?.ToString(),
            _ => throw new ArgumentOutOfRangeException(nameof(displayMode), displayMode, null)
        };

    /// <summary>
    /// Gets the reason why this item is unavailable.
    /// </summary>
    public string UnavailableReason
        => IsUnavailable ? Title : null;

    /// <summary>
    /// Gets the value name including its optional unavailability reason.
    /// </summary>
    /// <returns>The value name to display in the swatch selection label.</returns>
    public string GetSwatchDisplayName()
        => UnavailableReason.HasValue() ? $"{Name} — {UnavailableReason}" : Name;

    /// <summary>
    /// Gets the accessible item label including its optional title.
    /// </summary>
    /// <returns>The accessible label.</returns>
    public string GetAccessibleLabel()
    {
        var label = GetItemLabel();
        return Title.HasValue() ? $"{label} - {Title}" : label;
    }

    public abstract string GetItemLabel();
}
