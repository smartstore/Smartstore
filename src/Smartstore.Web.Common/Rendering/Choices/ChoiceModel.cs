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

    public string CustomData { get; set; }

    #region Choice box display

    /// <summary>
    /// Size of the boxes. Only applicable to <see cref="AttributeControlType.Boxes"/>.
    /// </summary>
    public ChoiceBoxSize BoxSize { get; set; } = ChoiceBoxSize.Medium;

    /// <summary>
    /// Shape of the boxes. Only applicable to <see cref="AttributeControlType.Boxes"/>.
    /// </summary>
    public ChoiceBoxShape BoxShape { get; set; } = ChoiceBoxShape.Rounded;

    /// <summary>
    /// A value indicating whether to render the value name within the box.
    /// Ignored if the boxes are not eligible for the portrait layout, because the name is the box content anyway.
    /// </summary>
    public bool ShowValueName { get; set; }

    /// <summary>
    /// Determines whether and how to render the value price within the box.
    /// </summary>
    public ChoiceBoxPriceDisplay PriceDisplay { get; set; }

    /// <summary>
    /// A value indicating whether the portrait layout is applicable at all, which requires
    /// every value to provide a color or an image. Boxes with a plain text label are excluded.
    /// </summary>
    public bool CanUsePortraitLayout
        => AttributeControlType == AttributeControlType.Boxes
            && Values.Count > 0
            && Values.All(x => x.HasSwatch);

    /// <summary>
    /// A value indicating whether the boxes are rendered as portrait boxes, i.e. with a caption below the swatch.
    /// </summary>
    public bool UsePortraitLayout
        => CanUsePortraitLayout && (ShowValueName || PriceDisplay != ChoiceBoxPriceDisplay.None);

    /// <summary>
    /// A value indicating whether a price is rendered as a second line within a compact box.
    /// </summary>
    public bool UseStackedLayout
        => !CanUsePortraitLayout && PriceDisplay != ChoiceBoxPriceDisplay.None;

    /// <summary>
    /// Gets the currently selected item, if any.
    /// </summary>
    public ChoiceItemModel SelectedValue
        => Values?.FirstOrDefault(x => x.IsPreSelected);

    #endregion

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