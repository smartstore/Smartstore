using NUnit.Framework;
using Smartstore.Core.Common;
using Smartstore.Web.Rendering.Choices;

namespace Smartstore.Web.Tests.Common.Rendering.Choices;

[TestFixture]
public class ChoiceItemModelTests
{
    [Test]
    public void Can_generate_single_color_css()
    {
        var item = new TestChoiceItemModel { Color = "#123456" };

        Assert.That(item.GetSwatchColorCss(), Is.EqualTo("background-color: #123456;"));
    }

    [Test]
    public void Can_generate_multicolor_css()
    {
        var item = new TestChoiceItemModel
        {
            Color = "#ff0000",
            AdditionalColors = ["#00ff00", "#0000ff"]
        };

        Assert.That(item.GetSwatchColorCss(), Is.EqualTo(
            "background-color: #ff0000;background-image: linear-gradient(135deg, " +
            "#ff0000 0%, #ff0000 33.3333%, " +
            "#00ff00 33.3333%, #00ff00 66.6667%, " +
            "#0000ff 66.6667%, #0000ff 100%);"));
    }

    [Test]
    public void Ignores_empty_transparent_and_excess_additional_colors()
    {
        var item = new TestChoiceItemModel
        {
            Color = "#111111",
            AdditionalColors = [null, "transparent", "#222222", "#333333", "#444444", "#555555"]
        };

        var css = item.GetSwatchColorCss();

        Assert.That(css, Does.Contain("#444444"));
        Assert.That(css, Does.Not.Contain("#555555"));
        Assert.That(css, Does.Not.Contain("transparent"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("transparent")]
    public void Does_not_generate_css_without_primary_color(string color)
    {
        var item = new TestChoiceItemModel
        {
            Color = color,
            AdditionalColors = ["#123456"]
        };

        Assert.That(item.GetSwatchColorCss(), Is.Null);
    }

    [TestCase(12.5, "+€12.50")]
    [TestCase(-12.5, "-€12.50")]
    public void Can_format_signed_price_adjustment(decimal amount, string expected)
    {
        var currency = new Currency
        {
            CurrencyCode = "EUR",
            DisplayLocale = "en-US",
            CustomFormatting = "€0.00"
        };
        var item = new TestChoiceItemModel
        {
            PriceAdjustment = new Money(amount, currency)
        };

        Assert.That(item.GetPriceAdjustmentText(), Is.EqualTo(expected));
    }

    [Test]
    public void Does_not_format_missing_or_zero_price_adjustment()
    {
        var item = new TestChoiceItemModel();

        Assert.That(item.GetPriceAdjustmentText(), Is.Null);

        item.PriceAdjustment = new Money(0, new Currency());

        Assert.That(item.GetPriceAdjustmentText(), Is.Null);
    }

    [Test]
    public void Resolves_swatch_price_according_to_display_mode()
    {
        var currency = new Currency
        {
            CurrencyCode = "EUR",
            DisplayLocale = "en-US",
            CustomFormatting = "€0.00"
        };
        var item = new TestChoiceItemModel
        {
            PriceAdjustment = new Money(12.5m, currency),
            SwatchPrice = new Money(99m, currency)
        };

        Assert.That(item.GetSwatchPriceText(SwatchPriceDisplayMode.None), Is.Null);
        Assert.That(item.GetSwatchPriceText(SwatchPriceDisplayMode.Adjustment), Is.EqualTo("+€12.50"));
        Assert.That(item.GetSwatchPriceText(SwatchPriceDisplayMode.FinalPrice), Is.EqualTo("€99.00"));
    }

    [Test]
    public void Builds_swatch_card_value_with_quantity()
    {
        var item = new TestChoiceItemModel
        {
            Name = "Red",
            QuantityInfo = 2
        };

        Assert.That(item.SwatchValueText, Is.EqualTo("2 x Red"));
    }

    [Test]
    public void Adds_compare_and_base_price_only_to_final_price_card_label()
    {
        var currency = new Currency
        {
            CurrencyCode = "EUR",
            DisplayLocale = "en-US",
            CustomFormatting = "€0.00"
        };
        var item = new TestChoiceItemModel
        {
            Name = "Red",
            PriceAdjustment = new Money(12.5m, currency),
            SwatchPrice = new Money(99m, currency),
            SwatchComparePrice = new Money(119m, currency),
            SwatchBasePriceInfo = "€9.90 / kg"
        };

        Assert.That(item.GetSwatchCardLabel(SwatchPriceDisplayMode.Adjustment), Is.EqualTo("Red, +€12.50"));
        Assert.That(item.GetSwatchCardLabel(SwatchPriceDisplayMode.FinalPrice), Is.EqualTo("Red, €99.00, €119.00, €9.90 / kg"));
    }

    [Test]
    public void Adds_unavailability_reason_to_swatch_display_name()
    {
        var item = new TestChoiceItemModel
        {
            Name = "Red",
            Title = "Out of stock"
        };

        Assert.That(item.GetSwatchDisplayName(), Is.EqualTo("Red"));
        Assert.That(item.UnavailableReason, Is.Null);

        item.IsUnavailable = true;

        Assert.That(item.GetSwatchDisplayName(), Is.EqualTo("Red — Out of stock"));
        Assert.That(item.UnavailableReason, Is.EqualTo("Out of stock"));
    }

    private sealed class TestChoiceItemModel : ChoiceItemModel
    {
        public override string GetItemLabel() => Name;
    }
}
