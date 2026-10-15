using NUnit.Framework;
using Smartstore.IconKit;

namespace Smartstore.Core.Tests.Content.Media.Icons;

[TestFixture]
public class IconOptionsTests
{
    [Test]
    public void CssClass_Appends_To_Existing_Attributes_And_Clone_Is_Independent()
    {
        var options = new IconOptions { Attributes = { ["class"] = "existing" } };
        options.CssClass = " added ";
        Assert.That(options.Attributes["class"], Is.EqualTo("existing added"));
        Assert.That(options.CssClass, Is.EqualTo("existing added"));
        options.CssClass = null;
        options.CssClass = "";
        Assert.That(options.CssClass, Is.EqualTo("existing added"));

        var clone = options.Clone();
        clone.CssClass = "cloned";
        Assert.That(clone.CssClass, Is.EqualTo("existing added cloned"));
        Assert.That(options.CssClass, Is.EqualTo("existing added"));
        options.Attributes["class"] = "replaced";
        Assert.That(options.CssClass, Is.EqualTo("replaced"));
    }
}
