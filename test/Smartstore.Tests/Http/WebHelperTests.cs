using NUnit.Framework;
using Smartstore.Http;

namespace Smartstore.Tests.Http;

[TestFixture]
public class WebHelperTests
{
    [TestCase("/fonts/Inter-Regular.ttf", "https://shop.example/fonts/Inter-Regular.ttf")]
    [TestCase("fonts/Inter-Regular.ttf", "https://shop.example/fonts/Inter-Regular.ttf")]
    [TestCase("//cdn.example/font.ttf", "https://cdn.example/font.ttf")]
    [TestCase("https://cdn.example/font.ttf", "https://cdn.example/font.ttf")]
    public void MakeAllUrlsAbsolute_preserves_css_url_syntax(string url, string expectedUrl)
    {
        var html = $"<style>@font-face {{ src: url('{url}') format('truetype'); }}</style>";
        var expected = $"<style>@font-face {{ src: url('{expectedUrl}') format('truetype'); }}</style>";

        Assert.That(WebHelper.MakeAllUrlsAbsolute(html, "https", "shop.example"), Is.EqualTo(expected));
    }

    [Test]
    public void MakeAllUrlsAbsolute_rewrites_each_font_source_on_the_same_line()
    {
        const string html = "<style>@font-face { src: url('/font.woff2') format('woff2'), url('/font.ttf') format('truetype'); }</style>";
        const string expected = "<style>@font-face { src: url('https://shop.example/font.woff2') format('woff2'), url('https://shop.example/font.ttf') format('truetype'); }</style>";

        Assert.That(WebHelper.MakeAllUrlsAbsolute(html, "https", "shop.example"), Is.EqualTo(expected));
    }

    [Test]
    public void MakeAllUrlsAbsolute_preserves_html_attributes_and_inline_styles()
    {
        const string html = "<a href=\"/page\"><img src='/image.png' style=\"background: url('/background.png')\"></a>";
        const string expected = "<a href=\"https://shop.example/page\"><img src='https://shop.example/image.png' style=\"background: url('https://shop.example/background.png')\"></a>";

        Assert.That(WebHelper.MakeAllUrlsAbsolute(html, "https", "shop.example"), Is.EqualTo(expected));
    }
}
