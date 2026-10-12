using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Smartstore.Bootstrapping;
using Smartstore.Caching;
using Smartstore.Engine;
using Smartstore.IconKit;

namespace Smartstore.Web.Tests.Common.TagHelpers;

/// <summary>
/// Exercises configured paths and real endpoint binding without starting an HTTP listener.
/// </summary>
[TestFixture]
public class IconKitRegistrationTests
{
    [Test]
    public async Task Registration_Resolves_Paths_And_Preserves_Absolute_Paths()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(Mock.Of<IApplicationContext>());
        builder.Services.AddSingleton(Mock.Of<ICacheManager>());
        builder.Services.AddIconKit(o => o.SourcePath = "App_Data/Icons");
        var absoluteCache = Path.Combine(Path.GetTempPath(), "iconkit-registration-test");
        builder.Services.AddIconKit(o => o.CachePath = absoluteCache);
        await using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<IconKitOptions>>().Value;
        Assert.That(options.SourcePath, Is.EqualTo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Icons")));
        Assert.That(options.CachePath, Is.EqualTo(absoluteCache));
        Assert.That(builder.Services.Count(x => x.ServiceType == typeof(IIconService)), Is.EqualTo(1));
        Assert.That(builder.Services.Count(x => x.ServiceType == typeof(IFileProvider) && Equals(x.ServiceKey, typeof(IconService))), Is.EqualTo(1));
    }

    [Test]
    public async Task Endpoints_Bind_Optional_Arguments_And_Reject_Invalid_Modifiers()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(Mock.Of<IApplicationContext>());
        builder.Services.AddSingleton(Mock.Of<ICacheManager>());
        builder.Services.AddIconKit(o => o.RoutePrefix = "/assets/glyphs");
        builder.Services.AddSingleton(Mock.Of<IIconKitService>());
        builder.Services.AddSingleton(Mock.Of<IIconBrowser>());
        var icons = new Mock<IIconService>();
        var icon = new IconInfo { Name = "cart" };
        icons.Setup(x => x.GetIconAsync("cart", null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(icon);
        var renderer = new Mock<IIconRenderer>();
        renderer.Setup(x => x.RenderAsync(icon, It.IsAny<IconOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(new TagBuilder("svg"));
        builder.Services.AddSingleton(icons.Object);
        builder.Services.AddSingleton(renderer.Object);
        await using var app = builder.Build();
        app.MapIconKit();
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(x => x.Endpoints).ToArray();
        var render = endpoints.Single(x => x.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "IconKit.Render");
        Assert.That(((RouteEndpoint)render).RoutePattern.RawText, Is.EqualTo("/assets/glyphs/render"));
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = "GET";
        context.Request.QueryString = new QueryString("?name=cart");
        context.Response.Body = new MemoryStream();
        await render.RequestDelegate(context);
        Assert.That(context.Response.StatusCode, Is.EqualTo(200));
        Assert.That(context.Response.ContentType, Is.EqualTo("image/svg+xml"));
        icons.VerifyAll();
        renderer.VerifyAll();

        var invalid = new DefaultHttpContext { RequestServices = app.Services };
        invalid.Request.Method = "GET";
        invalid.Request.QueryString = new QueryString("?name=cart&rotate=invalid");
        invalid.Response.Body = new MemoryStream();
        await render.RequestDelegate(invalid);
        Assert.That(invalid.Response.StatusCode, Is.EqualTo(400));

        var sprite = endpoints.Single(x => x.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "IconKit.Kit");
        Assert.That(sprite.Metadata.GetMetadata<HttpMethodMetadata>().HttpMethods, Is.EquivalentTo(new[] { "GET", "HEAD" }));
    }
}
