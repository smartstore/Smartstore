using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Seo;
using Smartstore.Core.Widgets;
using Smartstore.Web.Infrastructure.Seo;

namespace Smartstore.Web.Tests.Web;

[TestFixture]
public class HomepageCanonicalTests
{
    [Test]
    public void Disabled_setting_does_not_generate_homepage_canonical()
    {
        var assets = new Mock<IPageAssetBuilder>();
        var urls = new Mock<IUrlHelper>();

        HomepageCanonical.Register(assets.Object, new SeoSettings { CanonicalUrlsEnabled = false }, urls.Object, "https");

        assets.VerifyNoOtherCalls();
        urls.VerifyNoOtherCalls();
    }

    [TestCase("https", "https://example.test/")]
    [TestCase("https", "https://example.test/de/")]
    [TestCase("http", "http://example.test/")]
    public void Enabled_setting_uses_absolute_named_homepage_route_with_no_query_parameters(
        string scheme,
        string expectedUrl)
    {
        var assets = new Mock<IPageAssetBuilder>();
        var urls = new Mock<IUrlHelper>();

        UrlRouteContext requestedRoute = null;
        urls.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>()))
            .Callback<UrlRouteContext>(x => requestedRoute = x)
            .Returns(expectedUrl);

        HomepageCanonical.Register(assets.Object, new SeoSettings { CanonicalUrlsEnabled = true }, urls.Object, scheme);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requestedRoute, Is.Not.Null);
            Assert.That(requestedRoute.RouteName, Is.EqualTo("Homepage"));
            Assert.That(requestedRoute.Protocol, Is.EqualTo(scheme));
            Assert.That(new RouteValueDictionary(requestedRoute.Values), Is.Empty);
        }

        var expectedTag = $"<link rel=\"canonical\" href=\"{expectedUrl}\" />";
        assets.Verify(x => x.AddHtmlContent(
            "head_canonical",
            It.Is<Microsoft.AspNetCore.Html.IHtmlContent>(content => content.ToString() == expectedTag),
            expectedUrl,
            false), Times.Once);

        assets.VerifyNoOtherCalls();
        urls.Verify(x => x.RouteUrl(It.IsAny<UrlRouteContext>()), Times.Once);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("  ")]
    [TestCase("\t")]
    [TestCase("\r\n")]
    public void Missing_named_route_does_not_render_an_empty_canonical_link(string generatedUrl)
    {
        var assets = new Mock<IPageAssetBuilder>();
        var urls = new Mock<IUrlHelper>();
        urls.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns(generatedUrl);

        HomepageCanonical.Register(assets.Object, new SeoSettings { CanonicalUrlsEnabled = true }, urls.Object, "https");

        assets.VerifyNoOtherCalls();
        urls.Verify(x => x.RouteUrl(It.IsAny<UrlRouteContext>()), Times.Once);
    }

    [TestCase("https://example.test/")]
    [TestCase("https://www.example.test/")]
    [TestCase("http://localhost:5000/")]
    [TestCase("https://example.test:8443/")]
    [TestCase("https://shop-one.example.test/")]
    [TestCase("https://shop-two.example.test/")]
    [TestCase("https://example.test/de/")]
    [TestCase("https://example.test/fr/")]
    [TestCase("https://example.test/ar/")]
    [TestCase("https://example.test/tr/")]
    [TestCase("https://example.test/zh-CN/")]
    [TestCase("https://example.test/tenant/")]
    [TestCase("https://example.test/shop/de/")]
    [TestCase("https://example.test/stores/42/")]
    [TestCase("https://example.test/%D8%A7%D9%84%D9%85%D8%AA%D8%AC%D8%B1/")]
    [TestCase("https://xn--bcher-kva.example/")]
    [TestCase("http://example.test/")]
    public void Canonical_html_is_correct_for_localized_virtual_directory_and_store_host_routes(string absoluteHomeUrl)
    {
        var assets = new Mock<IPageAssetBuilder>();
        var urls = new Mock<IUrlHelper>();
        Microsoft.AspNetCore.Html.IHtmlContent actualTag = null;
        string actualKey = null;
        UrlRouteContext actualRoute = null;

        urls.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>()))
            .Callback<UrlRouteContext>(x => actualRoute = x)
            .Returns(absoluteHomeUrl);
        assets.Setup(x => x.AddHtmlContent(
                "head_canonical",
                It.IsAny<Microsoft.AspNetCore.Html.IHtmlContent>(),
                It.IsAny<string>(),
                false))
            .Callback<string, Microsoft.AspNetCore.Html.IHtmlContent, string, bool>(
                (zone, html, key, prepend) =>
                {
                    actualTag = html;
                    actualKey = key;
                });

        var protocol = absoluteHomeUrl.StartsWith("https:", System.StringComparison.Ordinal) ? "https" : "http";
        HomepageCanonical.Register(assets.Object, new SeoSettings { CanonicalUrlsEnabled = true }, urls.Object, protocol);

        using (var writer = new System.IO.StringWriter())
        {
            actualTag?.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
            Assert.That(writer.ToString(), Is.EqualTo($"<link rel=\"canonical\" href=\"{absoluteHomeUrl}\" />"));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actualRoute, Is.Not.Null);
            Assert.That(actualRoute.RouteName, Is.EqualTo("Homepage"));
            Assert.That(actualRoute.Protocol, Is.EqualTo(protocol));
            Assert.That(new RouteValueDictionary(actualRoute.Values), Is.Empty);
            Assert.That(actualKey, Is.EqualTo(absoluteHomeUrl));
            Assert.That(assets.Invocations.Count, Is.EqualTo(1));
            Assert.That(urls.Invocations.Count, Is.EqualTo(1));
        }
    }

    [Test]
    public void Disabling_the_setting_between_requests_stops_registering_a_canonical()
    {
        var assets = new Mock<IPageAssetBuilder>();
        var urls = new Mock<IUrlHelper>();
        var settings = new SeoSettings { CanonicalUrlsEnabled = true };
        urls.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("https://example.test/");

        HomepageCanonical.Register(assets.Object, settings, urls.Object, "https");
        settings.CanonicalUrlsEnabled = false;
        HomepageCanonical.Register(assets.Object, settings, urls.Object, "https");

        assets.Verify(x => x.AddHtmlContent(
            "head_canonical",
            It.IsAny<Microsoft.AspNetCore.Html.IHtmlContent>(),
            "https://example.test/",
            false), Times.Once);
        urls.Verify(x => x.RouteUrl(It.IsAny<UrlRouteContext>()), Times.Once);
    }

    [Test]
    public void Enabling_the_setting_between_requests_starts_registering_a_canonical()
    {
        var assets = new Mock<IPageAssetBuilder>();
        var urls = new Mock<IUrlHelper>();
        var settings = new SeoSettings { CanonicalUrlsEnabled = false };
        urls.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("https://example.test/");

        HomepageCanonical.Register(assets.Object, settings, urls.Object, "https");
        settings.CanonicalUrlsEnabled = true;
        HomepageCanonical.Register(assets.Object, settings, urls.Object, "https");

        assets.Verify(x => x.AddHtmlContent(
            "head_canonical",
            It.IsAny<Microsoft.AspNetCore.Html.IHtmlContent>(),
            "https://example.test/",
            false), Times.Once);
        urls.Verify(x => x.RouteUrl(It.IsAny<UrlRouteContext>()), Times.Once);
    }

    [Test]
    public void Disabled_canonical_does_not_touch_route_service_even_if_it_would_throw()
    {
        var assets = new Mock<IPageAssetBuilder>();
        var urls = new Mock<IUrlHelper>();
        urls.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>()))
            .Throws(new System.InvalidOperationException("Routes not initialized"));

        Assert.DoesNotThrow(() => HomepageCanonical.Register(
            assets.Object,
            new SeoSettings { CanonicalUrlsEnabled = false },
            urls.Object,
            "https"));

        urls.VerifyNoOtherCalls();
        assets.VerifyNoOtherCalls();
    }

    [Test]
    public void Route_generation_errors_are_not_silently_hidden()
    {
        var assets = new Mock<IPageAssetBuilder>();
        var urls = new Mock<IUrlHelper>();
        urls.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>()))
            .Throws(new System.InvalidOperationException("Homepage route not available"));

        Assert.Throws<System.InvalidOperationException>(() => HomepageCanonical.Register(
            assets.Object,
            new SeoSettings { CanonicalUrlsEnabled = true },
            urls.Object,
            "https"));

        assets.VerifyNoOtherCalls();
        urls.Verify(x => x.RouteUrl(It.IsAny<UrlRouteContext>()), Times.Once);
    }

    [Test]
    public void Two_stores_render_independent_homepage_canonicals()
    {
        var assetsOne = new Mock<IPageAssetBuilder>();
        var assetsTwo = new Mock<IPageAssetBuilder>();
        var urlsOne = new Mock<IUrlHelper>();
        var urlsTwo = new Mock<IUrlHelper>();
        urlsOne.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("https://one.example.test/en/");
        urlsTwo.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("https://two.example.test/ar/");

        var settings = new SeoSettings { CanonicalUrlsEnabled = true };
        HomepageCanonical.Register(assetsOne.Object, settings, urlsOne.Object, "https");
        HomepageCanonical.Register(assetsTwo.Object, settings, urlsTwo.Object, "https");

        assetsOne.Verify(x => x.AddHtmlContent(
            "head_canonical",
            It.IsAny<Microsoft.AspNetCore.Html.IHtmlContent>(),
            "https://one.example.test/en/",
            false), Times.Once);
        assetsTwo.Verify(x => x.AddHtmlContent(
            "head_canonical",
            It.IsAny<Microsoft.AspNetCore.Html.IHtmlContent>(),
            "https://two.example.test/ar/",
            false), Times.Once);
        urlsOne.Verify(x => x.RouteUrl(It.IsAny<UrlRouteContext>()), Times.Once);
        urlsTwo.Verify(x => x.RouteUrl(It.IsAny<UrlRouteContext>()), Times.Once);
    }
}
