using System.IO;
using System.Text.Encodings.Web;
using NUnit.Framework;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.Tests.Common.Rendering;

[TestFixture]
public class EncodingTests
{
    [Test]
    [TestCase("test<img src=x onerror=alert(document.domain)>", "<span class=\"highlight\">test</span>&lt;img src=x onerror=alert(document.domain)&gt;")]
    [TestCase("Test and test", "<span class=\"highlight\">Test</span> and <span class=\"highlight\">test</span>")]
    [TestCase("before <&> after", "before <span class=\"highlight\">&lt;&amp;&gt;</span> after", "<&>")]
    [TestCase("foo<img src=x onerror=alert(1)>", "foo&lt;img src=x onerror=alert(1)&gt;", null, false)]
    [TestCase("test<img src=x onerror=alert(1)>", "test&lt;img src=x onerror=alert(1)&gt;", null)]
    public void Encodes_entity_picker_title(string title, string expected, string term = "test", bool highlight = true)
    {
        var model = new EntityPickerModel
        {
            HighlightSearchTerm = highlight,
            SearchTerm = term
        };

        using var writer = new StringWriter();
        var content = model.HighlightTitle(title);
        content.WriteTo(writer, HtmlEncoder.Default);

        Assert.That(writer.ToString(), Is.EqualTo(expected));
    }
}
