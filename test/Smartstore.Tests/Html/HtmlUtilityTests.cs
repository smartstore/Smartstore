#nullable enable

using NUnit.Framework;
using Smartstore.Utilities.Html;

namespace Smartstore.Tests.Html;

[TestFixture]
public class HtmlUtilityTests
{
    [TestCase(null, true)]
    [TestCase("", true)]
    [TestCase(" \t\r\n", true)]
    [TestCase("&nbsp;", true)]
    [TestCase("<p>&nbsp;</p>", true)]
    [TestCase("<div><br></div>", true)]
    [TestCase("text", false)]
    [TestCase("<p>text</p>", false)]
    [TestCase("<img src=\"image.jpg\">", false)]
    public void IsEmptyHtml_detects_empty_html(string? html, bool expected)
    {
        Assert.That(HtmlUtility.IsEmptyHtml(html), Is.EqualTo(expected));
    }

    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase("plain text", false)]
    [TestCase("<p>text", false)]
    [TestCase("</p>", false)]
    [TestCase("<p>text</p>", true)]
    [TestCase("<br><p>text</p>", true)]
    [TestCase("<broken <p>text</p>", true)]
    public void IsHtml_detects_opening_and_closing_tags(string? input, bool expected)
    {
        Assert.That(HtmlUtility.IsHtml(input!), Is.EqualTo(expected));
    }
}
