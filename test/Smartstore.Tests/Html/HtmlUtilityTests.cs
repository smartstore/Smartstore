#nullable enable

using System;
using System.Collections.Generic;
using System.Web;
using AngleSharp.Html.Parser;
using NUnit.Framework;
using Smartstore.Utilities.Html;

namespace Smartstore.Tests.Html;

[TestFixture]
public class HtmlUtilityTests
{
    [Test]
    public void SanitizeHtml_preserves_restricted_comment_profile()
    {
        const string html = "<b style=\"color:red\" class=\"admin\" onclick=\"alert(1)\">text</b><img src=\"/image.png\"><script>alert(1)</script>";

        Assert.That(HtmlUtility.SanitizeHtml(html, HtmlSanitizerOptions.UserCommentSuitable), Is.EqualTo("<b>text</b>"));
    }

    [Test]
    public void SanitizeHtml_preserves_default_css_and_url_restrictions()
    {
        const string html = "<p style=\"color:red;font-size:16px\"><a href=\"https://example.com\">link</a><img src=\"/img.png\"></p>";

        Assert.That(HtmlUtility.SanitizeHtml(html), Is.EqualTo("<p><a>link</a><img src=\"/img.png\"></p>"));
    }

    [TestCase("https://example.com", true)]
    [TestCase("http://example.com", true)]
    [TestCase("/topic", true)]
    [TestCase("javascript:alert(1)", false)]
    [TestCase("JaVaScRiPt:alert(1)", false)]
    [TestCase("java&#x73;cript:alert(1)", false)]
    [TestCase("data:text/html,test", false)]
    public void SanitizeHtml_checks_link_schemes_when_explicitly_allowed(string href, bool allowed)
    {
        var options = new HtmlSanitizerOptions
        {
            AllowedAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "class", "href", "rel" },
            AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "http", "https" }
        };
        var html = $"<a href=\"{href}\" rel=\"nofollow\">link</a>";
        var expected = allowed ? html : "<a rel=\"nofollow\">link</a>";

        Assert.That(HtmlUtility.SanitizeHtml(html, options), Is.EqualTo(expected));
    }

    [Test]
    public void SanitizeHtml_document_removes_wrapper_event_attributes()
    {
        var html = HtmlUtility.SanitizeHtml("<html onclick=\"alert(1)\"><body onload=\"alert(1)\"><b>text</b></body></html>", false);

        Assert.That(html, Does.Contain("<b>text</b>"));
        Assert.That(html, Does.Not.Contain("onclick").And.Not.Contain("onload"));
    }

    [Test]
    public void RelativizeFontSizes_converts_inline_pixel_sizes()
    {
        var html = HtmlUtility.RelativizeFontSizes("<p style=\"font-size:24px\">text</p>", 16);

        Assert.That(html, Does.Contain("font-size: 1.5em"));
        Assert.That(html, Does.Contain(">text</p>"));
    }

    [TestCase("<template><img src=x onerror=alert(1)></template><b>text</b>")]
    [TestCase("<svg><style>&lt;/style&gt;&lt;img src=x onerror=alert(1)&gt;</style></svg><b>text</b>")]
    public void SanitizeHtml_removes_template_and_foreign_content(string input)
    {
        Assert.That(HtmlUtility.SanitizeHtml(input), Is.EqualTo("<b>text</b>"));
    }

    [Test]
    public void Email_css_inlining_preserves_relative_font_sizes_and_links()
    {
        // Reduced master.liquid/order.liquid structure: table hints, nested product link,
        // inline border/padding, and a rich-text topic processed before CSS inlining.
        var topic = HtmlUtility.RelativizeFontSizes("<p id=\"topic\" style=\"font-size:24px\">Delivery &amp; returns</p>", 16);
        var input = """
            <html><head><style>
            table.w-100 { width: 100%; -premailer-width: 100%; }
            td { vertical-align: top; -premailer-valign: top; color: #333333; }
            a { color: #0066cc; }
            .text-sm { font-size: 14px; }
            </style><style id="ignore">@media (max-width: 600px) { .w-100 { width: 100%; } }</style></head>
            <body><table class="w-100 order-items" cellpadding="10"><tbody><tr>
            <td style="border-top: 1px solid #eceeef; padding-right: 0; line-height: 1.3em" width="70%">
            <a class="order-item-link text-sm" href="/product?x=1&amp;y=2">Oak &amp; steel</a>
            <img src="/media/product.png" alt="Oak &amp; steel"></td><td class="text-sm">2</td>
            </tr></tbody></table>
            """ + topic + "</body></html>";
        using var preMailer = new PreMailer.Net.PreMailer(input, new Uri("https://shop.example/"));
        var inlined = preMailer.MoveCssInline(true, "#ignore").Html;
        using var document = new HtmlParser().ParseDocument(inlined);

        Assert.That(document.QuerySelectorAll("table > tbody > tr > td"), Has.Length.EqualTo(2));
        Assert.That(document.QuerySelector("table")!.GetAttribute("width"), Is.EqualTo("100%"));
        Assert.That(document.QuerySelector("td")!.GetAttribute("valign"), Is.EqualTo("top"));
        Assert.That(document.QuerySelector("td")!.GetAttribute("style"), Does.Contain("padding-right: 0").And.Contain("border-top: 1px solid #eceeef"));
        Assert.That(document.QuerySelector("a")!.GetAttribute("style"), Does.Contain("color: #0066cc").And.Contain("font-size: 14px"));
        Assert.That(document.QuerySelector("a")!.GetAttribute("href"), Is.EqualTo("/product?x=1&y=2"));
        Assert.That(document.QuerySelector("a")!.TextContent, Is.EqualTo("Oak & steel"));
        Assert.That(document.QuerySelector("img")!.GetAttribute("src"), Is.EqualTo("/media/product.png"));
        Assert.That(document.QuerySelector("#topic")!.GetAttribute("style"), Does.Contain("font-size: 1.5em"));
        Assert.That(document.QuerySelectorAll("style"), Has.Length.EqualTo(1));
        Assert.That(document.QuerySelector("style")!.Id, Is.EqualTo("ignore"));
    }

    [TestCase(null, "")]
    [TestCase("", "")]
    [TestCase("<p>Oak &amp; steel&nbsp;20</p><p>cm</p>", "Oak & steel\u00a020cm")]
    [TestCase("<!--note--><script>bad()</script><style>.bad{}</style><svg><text>hidden</text></svg><img alt='hidden'><p>Visible</p>", "Visible")]
    [TestCase("<p>First<br>Second</p><ul><li>Third</li></ul>", "FirstSecondThird")]
    [TestCase("<div><b>Unclosed &lt;tag&gt;", "Unclosed <tag>")]
    public void StripTags_preserves_text_for_descriptions_tickets_and_indexing(string? input, string expected)
    {
        Assert.That(HtmlUtility.StripTags(input), Is.EqualTo(expected));
    }

    [TestCase("  <p>Oak &amp;amp; steel</p>  ", "Oak & steel")]
    [TestCase("<p>&lt;b&gt;plain&lt;/b&gt;</p>", "<b>plain</b>")]
    public void RemoveHtml_trims_and_decodes_text_for_titles_and_search(string input, string expected)
    {
        Assert.That(input.RemoveHtml(), Is.EqualTo(expected));
    }

    [Test]
    public void StripTags_handles_encoded_import_and_export_descriptions()
    {
        const string input = "&lt;p&gt;Oak &amp;amp; steel&lt;/p&gt;";
        Assert.That(HtmlUtility.StripTags(HttpUtility.HtmlDecode(input)), Is.EqualTo("Oak & steel"));
    }

    [TestCase("<div><span>&nbsp;</span><br></div>", true)]
    [TestCase("<p>&#160;</p>", true)]
    [TestCase("<p><span>Content</span></p>", false)]
    [TestCase("<table><tr><td></td></tr></table>", false)]
    [TestCase("<iframe src='/video'></iframe>", false)]
    public void IsEmptyHtml_handles_editor_and_localized_content(string input, bool expected)
    {
        Assert.That(HtmlUtility.IsEmptyHtml(input), Is.EqualTo(expected));
    }

    [Test]
    public void IsEmptyHtml_keeps_long_markup_as_content()
    {
        Assert.That(HtmlUtility.IsEmptyHtml("<p>" + new string(' ', 501) + "</p>"), Is.False);
    }

    [TestCase("12px", "0.75em")]
    [TestCase("1.25em", "1.25em")]
    [TestCase("120%", "120%")]
    [TestCase("0px", "0px")]
    public void RelativizeFontSizes_preserves_other_styles_and_non_pixel_sizes(string size, string expected)
    {
        var html = HtmlUtility.RelativizeFontSizes($"<p style='font-size:{size};color:red'><b>Text</b></p>", 16);
        if (size == "12px")
        {
            Assert.That(html, Does.Contain($"font-size: {expected}").And.Contain("color: rgba(255, 0, 0, 1)"));
        }
        else
        {
            Assert.That(html, Does.Contain($"font-size:{expected};color:red"));
        }
        Assert.That(html, Does.Contain("<b>Text</b>"));
    }

    [TestCase(false, false, "")]
    [TestCase(true, false, "<b class=\"keep\">Text</b>")]
    [TestCase(true, true, "<b class=\"keep\" data-id=\"7\">Text</b>")]
    public void SanitizeHtml_respects_explicit_tag_class_and_data_attribute_options(bool keepChildren, bool allowData, string expected)
    {
        var options = new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string> { "b" },
            AllowedAttributes = new HashSet<string> { "class" },
            AllowedCssClasses = new HashSet<string> { "keep" },
            KeepChildNodes = keepChildren,
            AllowDataAttributes = allowData
        };
        Assert.That(HtmlUtility.SanitizeHtml("<div><b class='keep drop' data-id='7' title='drop'>Text</b></div>", options), Is.EqualTo(expected));
    }

    [Test]
    public void SanitizeHtml_applies_disallowed_tags_without_mutating_defaults()
    {
        var options = new HtmlSanitizerOptions { DisallowedTags = new HashSet<string> { "img" } };
        Assert.That(HtmlUtility.SanitizeHtml("<img src='/image.png'><b>Text</b>", options), Is.EqualTo("<b>Text</b>"));
        Assert.That(HtmlUtility.SanitizeHtml("<img src='/image.png'>"), Is.EqualTo("<img src=\"/image.png\">"));
    }

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
