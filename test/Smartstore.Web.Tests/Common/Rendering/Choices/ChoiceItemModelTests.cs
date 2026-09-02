using NUnit.Framework;
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

    private sealed class TestChoiceItemModel : ChoiceItemModel
    {
        public override string GetItemLabel() => Name;
    }
}
