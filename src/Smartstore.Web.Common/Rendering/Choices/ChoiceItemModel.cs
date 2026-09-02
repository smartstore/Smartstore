using System.Globalization;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Rendering.Choices;

public abstract class ChoiceItemModel : EntityModelBase
{
    public string Name { get; set; }
    public string SeName { get; set; }
    public string Title { get; set; }
    public string Alias { get; set; }
    public string Color { get; set; }
    public string PriceAdjustment { get; set; }
    public decimal PriceAdjustmentValue { get; set; }
    public string SwatchPrice { get; set; }
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

    public abstract string GetItemLabel();
}
