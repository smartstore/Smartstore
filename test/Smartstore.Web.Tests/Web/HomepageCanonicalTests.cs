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
    public void Missing_named_route_does_not_render_an_empty_canonical_link(string generatedUrl)
    {
        var assets = new Mock<IPageAssetBuilder>();
        var urls = new Mock<IUrlHelper>();
        urls.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns(generatedUrl);

        HomepageCanonical.Register(assets.Object, new SeoSettings { CanonicalUrlsEnabled = true }, urls.Object, "https");

        assets.VerifyNoOtherCalls();
        urls.Verify(x => x.RouteUrl(It.IsAny<UrlRouteContext>()), Times.Once);
    }
}
