using Smartstore.Core.Catalog.Attributes;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Rendering.Choices;

public abstract class ChoiceModel : EntityModelBase
{
    public AttributeControlType AttributeControlType { get; set; }

    public string Alias { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string TextPrompt { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsRequired { get; set; }
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Gets or sets the effective swatch size.
    /// </summary>
    public SwatchSize SwatchSize { get; set; } = SwatchSize.Medium;

    /// <summary>
    /// Gets or sets the configured swatch shape.
    /// </summary>
    public SwatchShape SwatchShape { get; set; } = SwatchShape.Rounded;

    /// <summary>
    /// Gets or sets the swatch height-to-width ratio.
    /// </summary>
    public decimal SwatchAspectRatio { get; set; } = 1m;

    /// <summary>
    /// Gets or sets a value indicating whether the value name is displayed in the swatch.
    /// </summary>
    public bool ShowValueNameInSwatch { get; set; }

    /// <summary>
    /// Gets or sets which price information is displayed in a swatch.
    /// </summary>
    public SwatchPriceDisplayMode SwatchPriceDisplay { get; set; }

    public string CustomData { get; set; }

    /// <summary>
    /// Allowed file extensions for customer uploaded files
    /// </summary>
    public List<string> AllowedFileExtensions { get; set; } = [];

    /// <summary>
    /// Selected value for textboxes
    /// </summary>
    public string TextValue { get; set; }

    /// <summary>
    /// Selected date value for datepicker
    /// </summary>
    public DateTime? SelectedDate { get; set; }
    public string UploadedFileGuid { get; set; }
    public string UploadedFileName { get; set; }

    public virtual List<ChoiceItemModel> Values { get; set; } = [];

    /// <summary>
    /// A value indicating whether the card layout is applicable at all, which requires
    /// every value to provide a color or an image. Boxes with a plain text label are excluded.
    /// </summary>
    public bool CanUseCardLayout
        => AttributeControlType == AttributeControlType.Boxes
            && Values is { Count: > 0 }
            && Values.All(x => x.HasSwatch);

    /// <summary>
    /// A value indicating whether the boxes are rendered as card boxes, i.e. with a caption below the swatch.
    /// </summary>
    public bool UseCardLayout
        => CanUseCardLayout
            && (ShowValueNameInSwatch || SwatchPriceDisplay != SwatchPriceDisplayMode.None)
            && SwatchSize >= SwatchSize.Large;

    /// <summary>
    /// Gets the effective swatch shape after applying safe layout fallbacks.
    /// </summary>
    public SwatchShape EffectiveSwatchShape
    {
        get
        {
            if (SwatchShape == SwatchShape.Circle && (UseCardLayout || Values.Any(x => !x.HasSwatch)))
            {
                return SwatchShape.Rounded;
            }

            return SwatchShape;
        }
    }

    /// <summary>
    /// Gets the effective positive swatch height-to-width ratio.
    /// </summary>
    public decimal EffectiveSwatchAspectRatio
        => SwatchAspectRatio > 0 ? SwatchAspectRatio : 1m;

    public abstract string BuildControlId();

    public virtual string GetLabel()
        => TextPrompt.NullEmpty() ?? Name;

    public virtual string GetDescription()
    {
        var containsImg = !Description.IsEmpty() && Description.Contains("<img");

        var desc = Description.RemoveHtml();
        if (containsImg || (desc.HasValue() && !desc.Trim().EqualsNoCase(GetLabel())))
        {
            return Description;
        }

        return null;
    }

    public virtual string GetFileUploadUrl(IUrlHelper url)
        => null;
}
