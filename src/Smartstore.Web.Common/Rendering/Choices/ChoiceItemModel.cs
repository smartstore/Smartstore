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

    /// <summary>
    /// The formatted final price of this item, including all price adjustments.
    /// </summary>
    public string Price { get; set; }

    /// <summary>
    /// A value indicating whether <see cref="Price"/> is not unambiguous yet, because the
    /// current attribute selection matches more than one combination. Rendered as a "from" price.
    /// </summary>
    public bool IsPriceEstimate { get; set; }
    public int QuantityInfo { get; set; }
    public bool IsPreSelected { get; set; }
    public bool IsDisabled { get; set; }
    public bool IsUnavailable { get; set; }
    public string ImageUrl { get; set; }
    public int DisplayOrder { get; set; }

    /// <summary>
    /// A value indicating whether this item can be represented by a color or image swatch.
    /// Items without a swatch (e.g. "M", "L", "XL") are not eligible for the portrait box layout.
    /// </summary>
    public bool HasSwatch
        => ImageUrl.HasValue() || (Color.HasValue() && Color != "transparent");

    public abstract string GetItemLabel();
}