using NUnit.Framework;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Web.Rendering.Choices;

namespace Smartstore.Web.Tests.Common.Rendering.Choices;

[TestFixture]
public class ChoiceModelTests
{
    [TestCase(SwatchSize.XSmall, "xs")]
    [TestCase(SwatchSize.Small, "sm")]
    [TestCase(SwatchSize.Medium, "md")]
    [TestCase(SwatchSize.Large, "lg")]
    [TestCase(SwatchSize.XLarge, "xl")]
    [TestCase(SwatchSize.XXLarge, "xxl")]
    public void Can_convert_swatch_size_to_css_token(SwatchSize value, string expected)
    {
        Assert.That(value.ToCssToken(), Is.EqualTo(expected));
    }

    [TestCase(SwatchShape.Rounded, "rounded")]
    [TestCase(SwatchShape.Rect, "rect")]
    [TestCase(SwatchShape.Circle, "circle")]
    public void Can_convert_swatch_shape_to_css_token(SwatchShape value, string expected)
    {
        Assert.That(value.ToCssToken(), Is.EqualTo(expected));
    }

    [Test]
    public void Cannot_use_card_layout_if_any_item_has_no_swatch()
    {
        var model = new TestChoiceModel
        {
            AttributeControlType = AttributeControlType.Boxes,
            Values =
            [
                new TestChoiceItemModel { Name = "Text" },
                new TestChoiceItemModel { Name = "Color", Color = "#fff" }
            ]
        };

        Assert.That(model.CanUseCardLayout, Is.False);
        Assert.That(model.UseCardLayout, Is.False);
    }

    [Test]
    public void Uses_card_layout_only_if_all_items_have_media_and_configuration_allows_it()
    {
        var model = new TestChoiceModel
        {
            AttributeControlType = AttributeControlType.Boxes,
            SwatchSize = SwatchSize.Large,
            ShowValueNameInSwatch = true,
            Values =
            [
                new TestChoiceItemModel { Color = "#fff" },
                new TestChoiceItemModel { ImageUrl = "/image.png" }
            ]
        };

        Assert.That(model.CanUseCardLayout, Is.True);
        Assert.That(model.UseCardLayout, Is.True);

        model.SwatchSize = SwatchSize.Medium;

        Assert.That(model.UseCardLayout, Is.False);
    }

    [Test]
    public void Falls_back_from_circle_for_cards_and_text_only_items()
    {
        var model = new TestChoiceModel
        {
            AttributeControlType = AttributeControlType.Boxes,
            SwatchShape = SwatchShape.Circle,
            SwatchSize = SwatchSize.Large,
            SwatchPriceDisplay = SwatchPriceDisplayMode.FinalPrice,
            Values = [new TestChoiceItemModel { Color = "#fff" }]
        };

        Assert.That(model.EffectiveSwatchShape, Is.EqualTo(SwatchShape.Rounded));

        model.SwatchPriceDisplay = SwatchPriceDisplayMode.None;

        Assert.That(model.EffectiveSwatchShape, Is.EqualTo(SwatchShape.Circle));

        model.Values = [new TestChoiceItemModel()];

        Assert.That(model.EffectiveSwatchShape, Is.EqualTo(SwatchShape.Rounded));
    }

    private sealed class TestChoiceModel : ChoiceModel
    {
        public override string BuildControlId() => "choice";
    }

    private sealed class TestChoiceItemModel : ChoiceItemModel
    {
        public override string GetItemLabel() => Name;
    }
}
