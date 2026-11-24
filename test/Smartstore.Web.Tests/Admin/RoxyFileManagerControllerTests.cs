using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using Smartstore.Admin.Controllers;

namespace Smartstore.Web.Tests.Admin;

[TestFixture]
public class RoxyFileManagerControllerTests
{
    [TestCase("COPYDIR")]
    [TestCase("COPYFILE")]
    [TestCase("CREATEDIR")]
    [TestCase("DELETEDIR")]
    [TestCase("DELETEFILE")]
    [TestCase("MOVEDIR")]
    [TestCase("MOVEFILE")]
    [TestCase("RENAMEDIR")]
    [TestCase("RENAMEFILE")]
    [TestCase("UPLOAD")]
    [TestCase("deletedir")]
    [TestCase("UNKNOWN")]
    public async Task Get_cannot_reach_mutating_operations(string action)
    {
        // No media services are supplied: rejection must happen before accessing them.
        var controller = (RoxyFileManagerController)RuntimeHelpers.GetUninitializedObject(typeof(RoxyFileManagerController));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.Request.Method = "GET";

        var result = await controller.ProcessRequest(a: action);

        Assert.That(result, Is.TypeOf<StatusCodeResult>());
        Assert.That(((StatusCodeResult)result).StatusCode, Is.EqualTo(405));
        Assert.That(controller.Response.Headers.Allow.ToString(), Is.EqualTo("POST"));
    }

    [Test]
    public void Endpoint_retains_get_and_post_and_inherits_antiforgery_validation()
    {
        var method = typeof(RoxyFileManagerController).GetMethod(nameof(RoxyFileManagerController.ProcessRequest));
        Assert.That(method.GetCustomAttribute<IgnoreAntiforgeryTokenAttribute>(), Is.Null);
        Assert.That(typeof(RoxyFileManagerController).GetCustomAttribute<IgnoreAntiforgeryTokenAttribute>(true), Is.Null);
        Assert.That(method.GetCustomAttribute<AcceptVerbsAttribute>().HttpMethods, Is.EquivalentTo(new[] { "GET", "POST" }));
        Assert.That(typeof(RoxyFileManagerController).GetCustomAttribute<AutoValidateAntiforgeryTokenAttribute>(true), Is.Not.Null);
    }

    [TestCase("GET", "none", true)]
    [TestCase("POST", "none", false)]
    [TestCase("POST", "invalid", false)]
    [TestCase("POST", "header", true)]
    [TestCase("POST", "form", true)]
    public async Task Inherited_filter_accepts_only_valid_post_tokens(string method, string tokenSource, bool allowed)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddAntiforgery(options => options.HeaderName = "X-XSRF-Token");
        services.AddControllersWithViews();
        using var provider = services.BuildServiceProvider();

        var issuingContext = new DefaultHttpContext { RequestServices = provider };
        var antiforgery = provider.GetRequiredService<IAntiforgery>();
        var tokens = antiforgery.GetAndStoreTokens(issuingContext);
        var http = new DefaultHttpContext { RequestServices = provider };
        http.Request.Method = method;
        http.Request.Headers.Cookie = issuingContext.Response.Headers.SetCookie.ToString().Split(';')[0];

        if (tokenSource == "header" || tokenSource == "invalid")
        {
            http.Request.Headers[tokens.HeaderName] = tokenSource == "header" ? tokens.RequestToken : "invalid";
        }
        else if (tokenSource == "form")
        {
            http.Request.ContentType = "multipart/form-data; boundary=test";
            http.Request.Form = new FormCollection(new Dictionary<string, StringValues>
            {
                [tokens.FormFieldName] = tokens.RequestToken
            });
        }

        var attribute = typeof(RoxyFileManagerController).GetCustomAttribute<AutoValidateAntiforgeryTokenAttribute>(true);
        var filter = attribute.CreateInstance(provider);
        var context = new AuthorizationFilterContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor(), new ModelStateDictionary()),
            new List<IFilterMetadata> { filter });

        await ((IAsyncAuthorizationFilter)filter).OnAuthorizationAsync(context);

        Assert.That(context.Result == null, Is.EqualTo(allowed));
        if (!allowed)
        {
            Assert.That(context.Result, Is.TypeOf<AntiforgeryValidationFailedResult>());
        }
    }
}
