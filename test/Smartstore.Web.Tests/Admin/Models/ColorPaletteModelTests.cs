using System.Linq;
using NUnit.Framework;
using Smartstore.Admin.Models.Common;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.Tests.Admin.Models;

[TestFixture]
public class ColorPaletteModelTests
{
    [Test]
    public void Can_create_palette_from_entity_colors()
    {
        var model = ColorPaletteModel.Create("#111111", [null, "", "#222222", "#333333"]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.Color, Is.EqualTo("#111111"));
            Assert.That(model.AdditionalColors, Is.EqualTo(["#222222", "#333333"]));
        }
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void Grid_uses_shared_swatch_rendering(int count)
    {
        string[] colors = ["#111111", "rgba(0, 255, 0, 0.5)", "#333333", "#444444"];
        var model = ColorPaletteModel.Create(" #111111 ",
            new[] { " transparent ", " " }.Concat(colors.Skip(1).Take(count - 1)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.SwatchColors, Is.EqualTo(colors.Take(count)));
            Assert.That(model.SwatchStyle, Is.EqualTo(SwatchColorHelper.GetCss(model.Color, model.AdditionalColors)));
            Assert.That(SwatchColorHelper.HasMultipleColors(model.Color, model.AdditionalColors), Is.EqualTo(count > 1));
            if (count > 1)
            {
                Assert.That(model.SwatchStyle, Does.Not.Contain("background-color"));
                Assert.That(model.SwatchStyle, Does.Contain("rgba(0, 255, 0, 0.5)"));
            }
        }
    }

    [TestCase(null)]
    [TestCase(" Transparent ")]
    public void Grid_requires_primary_color(string color)
    {
        var model = ColorPaletteModel.Create(color, ["#222222"]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.SwatchColors, Is.Empty);
            Assert.That(model.SwatchStyle, Is.Null);
        }
    }

    [Test]
    public void Can_normalize_and_limit_additional_colors()
    {
        var model1 = new ColorPaletteModel
        {
            AdditionalColors = [" #222222 ", null, "#333333", "#444444", "#555555"]
        };
        var model2 = new ColorPaletteModel { AdditionalColors = [null, ""] };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model1.GetAdditionalColors(), Is.EqualTo(["#222222", "#333333", "#444444"]));
            Assert.That(model2.GetAdditionalColors(), Is.Null);
        }
    }
}
