using NUnit.Framework;
using Smartstore.Admin.Models.Common;

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
