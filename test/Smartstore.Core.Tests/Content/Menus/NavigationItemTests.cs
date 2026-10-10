using NUnit.Framework;
using Smartstore.Core.Content.Menus;

namespace Smartstore.Core.Tests.Content.Menus;

[TestFixture]
public class NavigationItemTests
{
    [TestCase("phone", "bi", "bi:phone")]
    [TestCase("hi:phone!@sr", "bi", "hi:phone!@sr")]
    [TestCase("phone", null, "phone")]
    [TestCase(null, "bi", null)]
    [TestCase("", "bi", "")]
    [TestCase("far fa-phone", "bi", "far fa-phone")]
    public void Legacy_library_qualifies_only_unqualified_names(string icon, string library, string expected)
    {
#pragma warning disable CS0618 // Verify compatibility with extensions using the obsolete property.
        var item = new MenuItem { Icon = icon, IconLibrary = library };
        var reverseOrder = new MenuItem { IconLibrary = library, Icon = icon };
#pragma warning restore CS0618

        Assert.That(item.Icon, Is.EqualTo(expected));
        Assert.That(reverseOrder.Icon, Is.EqualTo(expected));
    }
}
