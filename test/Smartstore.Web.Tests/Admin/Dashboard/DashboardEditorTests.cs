using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.TagHelpers.Shared;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Primitives;
using Smartstore.Engine.Modularity;
using Smartstore.Core.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.Core.Widgets;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Smartstore.Admin.Controllers;
using Smartstore.Admin.Infrastructure.Dashboard;
using Smartstore.Admin.Models.Dashboard;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Web;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Core;
using Smartstore.Json;
using Smartstore.Web.Models;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Tests.Admin.Dashboard;

#nullable disable

/// <summary>
/// Exercises dashboard view selection, edit authorization and the read-permission filter.
/// </summary>
[TestFixture]
public class HomeControllerDashboardTests
{
    [TestCase(true, true)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    public async Task Configuration_Route_Checks_Widget_Availability_And_Policy(bool available, bool configurable)
    {
        var widget = new Mock<IDashboardWidget>();
        var descriptor = new DashboardWidgetDescriptor("test-widget", "Test")
        {
            DefaultSize = new DashboardWidgetSize(4, 1),
            MinSize = new DashboardWidgetSize(1, 1),
            MaxSize = new DashboardWidgetSize(12),
            Policy = new DashboardWidgetPolicy { AllowConfigure = configurable }
        };
        widget.SetupGet(x => x.Descriptor).Returns(descriptor);
        widget.Setup(x => x.IsAvailableAsync(It.IsAny<DashboardWidgetContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(available);
        var configuration = new PartialViewWidget("Configuration");
        widget.Setup(x => x.CreateConfigurationWidgetAsync(It.IsAny<DashboardWidgetContext>(), It.IsAny<DashboardWidgetInstance>()))
            .ReturnsAsync(configuration);
        var layout = new DashboardLayout(AdminDashboardLayoutProvider.Id)
        {
            Widgets = [new DashboardWidgetInstance("instance", descriptor.SystemName)
            {
                Policy = new DashboardWidgetPolicy { AllowConfigure = true },
                Positions = [new DashboardWidgetPosition { Size = new DashboardWidgetSize(4, 1) }]
            }]
        };
        Mock<IDashboardService> dashboard = null;
        var controller = CreateController(
            true,
            false,
            DashboardLayoutLayer.User,
            service =>
            {
                dashboard = service;
                service.Setup(x => x.GetWidget(descriptor.SystemName)).Returns(widget.Object);
                service.Setup(x => x.GetLayoutStateAsync(
                    layout.Id,
                    It.IsAny<Customer>(),
                    DashboardLayoutLayer.User,
                    false,
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new DashboardLayoutState { Layout = layout, Layer = DashboardLayoutLayer.User, Source = DashboardLayoutLayer.Global });
            });

        var changes = new DashboardLayoutChanges([new("instance", descriptor.SystemName, layout.Widgets[0].Positions, null)]);
        var result = await controller.DashboardConfigure("instance", descriptor.SystemName, DashboardLayoutLayer.User, JsonSerializer.Serialize(changes, SmartJsonOptions.CamelCased));

        if (available && configurable)
        {
            Assert.That(result, Is.TypeOf<PartialViewResult>());
            Assert.That(((PartialViewResult)result).Model, Is.SameAs(configuration));
            widget.Verify(x => x.ValidateSettingsAsync(It.IsAny<JsonObject>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            Assert.That(result, Is.TypeOf<ForbidResult>());
            widget.Verify(x => x.CreateConfigurationWidgetAsync(It.IsAny<DashboardWidgetContext>(), It.IsAny<DashboardWidgetInstance>()), Times.Never);
        }

        dashboard.Verify(x => x.SaveLayoutAsync(
            It.IsAny<DashboardLayout>(),
            It.IsAny<Customer>(),
            It.IsAny<DashboardLayoutLayer>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase("Count", "3", true)]
    [TestCase("Count", "51", false)]
    [TestCase("Count", "", false)]
    [TestCase("Count", "abc", false)]
    [TestCase("Fees[Payments.Example].Value", "2.75", true)]
    [TestCase("Fees[Payments.Example].Value", "101", false)]
    [TestCase("Fees[Payments.Example].Value", "", false)]
    [SetCulture("de-DE")]
    public async Task Configuration_Form_Binds_Validates_And_Renders_In_One_Request(string field, string value, bool valid)
    {
        var model = new PaymentMethodsDashboardWidgetSettings();
        var configuration = new PartialViewWidget("Configuration", model);
        var widget = new Mock<IDashboardWidget>();
        widget.SetupGet(x => x.Descriptor).Returns(new DashboardWidgetDescriptor("test-widget", "Test")
        {
            DefaultSize = new DashboardWidgetSize(4, 1),
            MinSize = new DashboardWidgetSize(1, 1),
            MaxSize = new DashboardWidgetSize(12),
            Policy = new DashboardWidgetPolicy { AllowConfigure = true }
        });
        widget.Setup(x => x.IsAvailableAsync(It.IsAny<DashboardWidgetContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        widget.Setup(x => x.CreateConfigurationWidgetAsync(It.IsAny<DashboardWidgetContext>(), It.IsAny<DashboardWidgetInstance>()))
            .ReturnsAsync(configuration);
        var instance = new DashboardWidgetInstance("instance", "test-widget")
        {
            Policy = new DashboardWidgetPolicy { AllowConfigure = true },
            Positions = [new DashboardWidgetPosition { Size = new DashboardWidgetSize(4, 1) }]
        };
        var layout = new DashboardLayout(AdminDashboardLayoutProvider.Id) { Widgets = [instance] };
        Mock<IDashboardService> dashboard = null;
        var controller = CreateController(
            true,
            false,
            DashboardLayoutLayer.User,
            service =>
            {
                dashboard = service;
                service.Setup(x => x.GetWidget("test-widget")).Returns(widget.Object);
                service.Setup(x => x.GetLayoutStateAsync(
                    layout.Id,
                    It.IsAny<Customer>(),
                    DashboardLayoutLayer.User,
                    false,
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new DashboardLayoutState { Layout = layout, Layer = DashboardLayoutLayer.User, Source = DashboardLayoutLayer.User });
                service.Setup(x => x.PrepareWidgetAsync(
                    It.IsAny<DashboardLayout>(),
                    "instance",
                    It.IsAny<Customer>(),
                    false,
                    It.IsAny<IReadOnlyDictionary<string, string>>(),
                    It.IsAny<CancellationToken>()))
                    .Returns((
                        DashboardLayout draft,
                        string id,
                        Customer customer,
                        bool refresh,
                        IReadOnlyDictionary<string, string> parameters,
                        CancellationToken token) =>
                        ValueTask.FromResult(new DashboardRenderItem
                        {
                            Instance = draft.Widgets.Single(), Descriptor = widget.Object.Descriptor,
                            Policy = widget.Object.Descriptor.Policy, Widget = new HtmlWidget("<div>Updated widget</div>")
                        }));
            });
        var invoker = new Mock<IWidgetInvoker<PartialViewWidget>>();
        invoker.Setup(x => x.InvokeAsync(It.IsAny<WidgetContext>(), It.IsAny<PartialViewWidget>()))
            .ReturnsAsync(new HtmlString("<div>Updated widget</div>"));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvcCore(options => options.ModelBinderProviders.Insert(0, new InvariantFloatingPointTypeModelBinderProvider())).AddDataAnnotations();
        services.AddFluentValidationAutoValidation();
        services.AddTransient<IValidator<PaymentMethodsDashboardWidgetSettings>, PaymentMethodsDashboardWidgetValidator>();
        services.AddSingleton(invoker.Object);
        using var provider = services.BuildServiceProvider();
        controller.HttpContext.RequestServices = provider;
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());
        controller.ControllerContext.ValueProviderFactories.Add(new FormValueProviderFactory());
        var fields = new Dictionary<string, StringValues>
        {
            ["Count"] = "7",
            ["Fees[Payments.Example].Value"] = "2.75",
            ["Fees[Payments.Example].Unit"] = "%"
        };
        fields[field] = value;
        controller.Request.ContentType = "application/x-www-form-urlencoded";
        controller.Request.Form = new FormCollection(fields);
        var parameters = new Dictionary<string, string> { ["period"] = "30" };

        var changes = new DashboardLayoutChanges([new(instance.Id, instance.WidgetSystemName, instance.Positions, instance.Settings)]);
        var result = await controller.DashboardConfigure(
            "instance",
            "test-widget",
            DashboardLayoutLayer.User,
            JsonSerializer.Serialize(changes, SmartJsonOptions.CamelCased),
            apply: true,
            parameters: parameters);

        if (valid)
        {
            var json = (JsonResult)result;
            var settings = (JsonObject)json.Value.GetType().GetProperty("settings").GetValue(json.Value);
            Assert.That(settings["count"].GetValue<int>(), Is.EqualTo(field == "Count" ? 3 : 7));
            Assert.That(settings["fees"]["Payments.Example"]["value"].GetValue<decimal>(), Is.EqualTo(2.75m));
            Assert.That(json.Value.GetType().GetProperty("html").GetValue(json.Value), Is.EqualTo("<div>Updated widget</div>"));
            Assert.That(controller.ModelState, Is.Empty);
            dashboard.Verify(x => x.PrepareWidgetAsync(
                layout,
                "instance",
                It.IsAny<Customer>(),
                false,
                parameters,
                It.IsAny<CancellationToken>()), Times.Once);
            invoker.Verify(x => x.InvokeAsync(
                It.IsAny<WidgetContext>(),
                It.Is<PartialViewWidget>(v => v.ViewName == "_DashboardWidget" && v.Model is DashboardRenderItem)), Times.Once);
        }
        else
        {
            Assert.That(((PartialViewResult)result).Model, Is.SameAs(configuration));
            Assert.That(controller.ModelState[field].Errors, Is.Not.Empty);
            Assert.That(controller.ModelState[field].AttemptedValue, Is.EqualTo(value));
            Assert.That(layout.Widgets.Single().Settings.ToJsonString(), Is.EqualTo(instance.Settings.ToJsonString()));
            dashboard.Verify(x => x.PrepareWidgetAsync(
                It.IsAny<DashboardLayout>(),
                It.IsAny<string>(),
                It.IsAny<Customer>(),
                It.IsAny<bool>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()), Times.Never);
            invoker.Verify(x => x.InvokeAsync(It.IsAny<WidgetContext>(), It.IsAny<PartialViewWidget>()), Times.Never);
        }

        dashboard.Verify(x => x.SaveLayoutAsync(
            It.IsAny<DashboardLayout>(),
            It.IsAny<Customer>(),
            It.IsAny<DashboardLayoutLayer>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Configuration_Route_Validates_The_Draft_And_Rejects_Unknown_Instances()
    {
        Mock<IDashboardService> dashboard = null;
        var controller = CreateController(true, false, DashboardLayoutLayer.User, service => dashboard = service);
        var result = await controller.DashboardConfigure("missing", "widget", DashboardLayoutLayer.User, "{\"widgets\":[]}");
        Assert.That(result, Is.TypeOf<NotFoundResult>());
        dashboard.Verify(x => x.ValidateDraftAsync(
            It.IsAny<DashboardLayout>(),
            It.IsAny<Customer>(),
            DashboardLayoutLayer.User,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase(false, DashboardLayoutLayer.Global, DashboardLayoutLayer.User)]
    [TestCase(true, DashboardLayoutLayer.Global, DashboardLayoutLayer.Global)]
    [TestCase(true, DashboardLayoutLayer.System, DashboardLayoutLayer.User)]
    public async Task Refresh_Uses_The_Authorized_Selected_View(bool manageGlobal, DashboardLayoutLayer selected, DashboardLayoutLayer expected)
    {
        Mock<IDashboardService> service = null;
        var controller = CreateController(false, manageGlobal, selected, mock => service = mock);
        var result = await controller.DashboardWidget("missing", true);
        Assert.That(result, Is.TypeOf<NotFoundResult>());
        service.Verify(x => x.GetLayoutStateAsync("admin-dashboard", It.IsAny<Customer>(), expected, false, It.IsAny<CancellationToken>()), Times.Once());
        service.Verify(x => x.PrepareWidgetAsync(
            It.IsAny<DashboardLayout>(),
            "missing",
            It.IsAny<Customer>(),
            true,
            It.IsAny<IReadOnlyDictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Once());
        service.Verify(x => x.SaveLayoutAsync(
            It.IsAny<DashboardLayout>(),
            It.IsAny<Customer>(),
            It.IsAny<DashboardLayoutLayer>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Test]
    public async Task Widget_Draft_Is_Validated_Without_Persisting()
    {
        Mock<IDashboardService> service = null;
        var controller = CreateController(true, false, DashboardLayoutLayer.User, mock => service = mock);
        var result = await controller.DashboardWidget("missing", changesJson: "{\"widgets\":[]}", layer: DashboardLayoutLayer.User);
        Assert.That(result, Is.TypeOf<NotFoundResult>());
        service.Verify(x => x.ValidateDraftAsync(
            It.IsAny<DashboardLayout>(),
            It.IsAny<Customer>(),
            DashboardLayoutLayer.User,
            It.IsAny<CancellationToken>()), Times.Once());
        service.Verify(x => x.SaveLayoutAsync(
            It.IsAny<DashboardLayout>(),
            It.IsAny<Customer>(),
            It.IsAny<DashboardLayoutLayer>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Test]
    public async Task Widget_Preview_Passes_Draft_Settings_To_The_Existing_Renderer()
    {
        Mock<IDashboardService> service = null;
        var widget = new LatestOrdersDashboardWidget(Mock.Of<IPermissionService>(), new LatestOrdersDashboardWidgetValidator());
        var controller = CreateController(
            true,
            false,
            DashboardLayoutLayer.User,
            mock =>
            {
                service = mock;
                mock.Setup(x => x.GetWidget(LatestOrdersDashboardWidget.SystemName)).Returns(widget);
            });
        var changes = new DashboardLayoutChanges([new DashboardWidgetPlacement(
            "latest",
            LatestOrdersDashboardWidget.SystemName,
            [new DashboardWidgetPosition { Size = new DashboardWidgetSize(4) }],
            new JsonObject { ["count"] = 12 })]);
        await controller.DashboardWidget(
            "latest",
            changesJson: JsonSerializer.Serialize(changes, SmartJsonOptions.CamelCased),
            layer: DashboardLayoutLayer.User);
        service.Verify(x => x.PrepareWidgetAsync(
            It.Is<DashboardLayout>(layout => layout.Widgets.Single().Settings["count"].GetValue<int>() == 12),
            "latest",
            It.IsAny<Customer>(),
            false,
            It.IsAny<IReadOnlyDictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Once());
        service.Verify(x => x.SaveLayoutAsync(
            It.IsAny<DashboardLayout>(),
            It.IsAny<Customer>(),
            It.IsAny<DashboardLayoutLayer>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Test]
    public void Widget_Draft_Requires_Edit_Permission()
    {
        var controller = CreateController(false, false, DashboardLayoutLayer.User);
        Assert.ThrowsAsync<AccessDeniedException>(async () => await controller.DashboardWidget(
            "widget",
            changesJson: "{\"widgets\":[]}",
            layer: DashboardLayoutLayer.User));
    }

    [Test]
    public async Task Widget_Draft_Rejects_Invalid_Json()
    {
        var controller = CreateController(true, false, DashboardLayoutLayer.User);
        Assert.That(
            await controller.DashboardWidget("widget", changesJson: "invalid", layer: DashboardLayoutLayer.User),
            Is.TypeOf<BadRequestResult>());
    }

    [Test]
    public void Refresh_Endpoint_Requires_Read_Permission_Post_And_Antiforgery()
    {
        var method = typeof(HomeController).GetMethod(nameof(HomeController.DashboardWidget));
        Assert.That(method.GetCustomAttribute<HttpPostAttribute>(), Is.Not.Null);
        Assert.That(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>(), Is.Not.Null);
        Assert.That(method.GetCustomAttribute<PermissionAttribute>(), Is.Not.Null);
    }

    [TestCase(false, DashboardLayoutLayer.System)]
    [TestCase(false, DashboardLayoutLayer.Global)]
    [TestCase(true, DashboardLayoutLayer.System)]
    [TestCase(true, DashboardLayoutLayer.Global)]
    public async Task Normal_Users_Ignore_Previous_View_Selections(bool personal, DashboardLayoutLayer selected)
    {
        Mock<IDashboardService> service = null;
        var controller = CreateController(personal, false, selected, mock => service = mock);
        var result = (ViewResult)await controller.Index();
        var model = (DashboardEditorModel)result.Model;
        Assert.That(model.State.Layer, Is.EqualTo(DashboardLayoutLayer.User));
        service.Verify(x => x.GetSelectedLayer(It.IsAny<string>(), It.IsAny<Customer>()), Times.Never());
        service.Verify(x => x.SaveLayoutAsync(
            It.IsAny<DashboardLayout>(),
            It.IsAny<Customer>(),
            It.IsAny<DashboardLayoutLayer>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    public void Normal_Users_Cannot_Select_The_Global_View(bool personal, bool global)
    {
        var controller = CreateController(personal, global, DashboardLayoutLayer.User);
        Assert.ThrowsAsync<AccessDeniedException>(async () => await controller.DashboardSelect(DashboardLayoutLayer.Global));
    }

    [Test]
    public async Task System_View_Is_No_Longer_Selectable()
    {
        var controller = CreateController(true, true, DashboardLayoutLayer.System);
        Assert.That(await controller.DashboardSelect(DashboardLayoutLayer.System), Is.TypeOf<BadRequestResult>());
        var result = (ViewResult)await controller.Index();
        Assert.That(((DashboardEditorModel)result.Model).State.Layer, Is.EqualTo(DashboardLayoutLayer.User));
    }

    [Test]
    public async Task Personal_Save_Does_Not_Persist_A_View_Selection()
    {
        Mock<IDashboardService> service = null;
        var controller = CreateController(true, false, DashboardLayoutLayer.Global, mock => service = mock);
        Assert.That(await controller.DashboardSave(DashboardLayoutLayer.User, "{\"widgets\":[]}", false), Is.TypeOf<RedirectToActionResult>());
        service.Verify(x => x.SaveLayoutAsync(
            It.IsAny<DashboardLayout>(),
            It.IsAny<Customer>(),
            DashboardLayoutLayer.User,
            false,
            It.IsAny<CancellationToken>()), Times.Once());
        service.Verify(x => x.SelectLayerAsync(
            It.IsAny<string>(),
            It.IsAny<Customer>(),
            It.IsAny<DashboardLayoutLayer>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Preview_Does_Not_Persist_And_Reset_Saves_Immediately(bool reset)
    {
        Mock<IDashboardService> service = null;
        var state = new DashboardLayoutState
        {
            Layout = new DashboardLayout("admin-dashboard"),
            Layer = DashboardLayoutLayer.User,
            Source = DashboardLayoutLayer.Global
        };
        var controller = CreateController(
            true,
            false,
            DashboardLayoutLayer.User,
            mock =>
            {
                service = mock;
                mock.Setup(x => x.GetLayoutStateAsync(
                    "admin-dashboard",
                    It.IsAny<Customer>(),
                    DashboardLayoutLayer.User,
                    reset,
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(state);
            });

        var result = reset
            ? await controller.DashboardSave(DashboardLayoutLayer.User, "invalid discarded JSON", true)
            : await controller.DashboardPreview(DashboardLayoutLayer.User, "{\"widgets\":[]}", "", "");

        if (reset)
        {
            var redirect = (RedirectToActionResult)result;
            Assert.That(redirect.ActionName, Is.EqualTo(nameof(HomeController.Index)));
            Assert.That(redirect.RouteValues.ContainsKey("edit"), Is.False);
        }
        else
        {
            var model = (DashboardEditorModel)((ViewResult)result).Model;
            Assert.That(model.State, Is.SameAs(state));
            Assert.That(model.IsDirty, Is.True);
        }

        service.Verify(x => x.GetLayoutStateAsync(
            It.IsAny<string>(),
            It.IsAny<Customer>(),
            It.IsAny<DashboardLayoutLayer>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);
        service.Verify(x => x.ValidateDraftAsync(
            state.Layout,
            It.IsAny<Customer>(),
            DashboardLayoutLayer.User,
            It.IsAny<CancellationToken>()), reset ? Times.Never() : Times.Once());
        service.Verify(x => x.SaveLayoutAsync(
            state.Layout,
            It.IsAny<Customer>(),
            DashboardLayoutLayer.User,
            reset,
            It.IsAny<CancellationToken>()), reset ? Times.Once() : Times.Never());
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Write_Error_Retains_The_Draft_Or_Reloads_The_Saved_Layout_After_Reset(bool reset)
    {
        Mock<IDashboardService> service = null;
        DashboardLayout saved = null;
        var controller = CreateController(
            true,
            false,
            DashboardLayoutLayer.User,
            mock =>
            {
                service = mock;
                mock.Setup(x => x.GetLayoutStateAsync(
                    "admin-dashboard",
                    It.IsAny<Customer>(),
                    DashboardLayoutLayer.User,
                    true,
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new DashboardLayoutState
                    {
                        Layout = new DashboardLayout("admin-dashboard"),
                        Layer = DashboardLayoutLayer.User,
                        Source = DashboardLayoutLayer.Global
                    });
                mock.Setup(x => x.SaveLayoutAsync(
                    It.IsAny<DashboardLayout>(),
                    It.IsAny<Customer>(),
                    DashboardLayoutLayer.User,
                    reset,
                    It.IsAny<CancellationToken>()))
                    .Callback<DashboardLayout, Customer, DashboardLayoutLayer, bool, CancellationToken>((layout, customer, layer, resetLayout, token) => saved = layout)
                    .ThrowsAsync(new System.IO.IOException("Test write failure."));
            });

        var result = (ViewResult)await controller.DashboardSave(DashboardLayoutLayer.User, "{\"widgets\":[]}", reset);
        var model = (DashboardEditorModel)result.Model;

        Assert.That(ReferenceEquals(model.State.Layout, saved), Is.EqualTo(!reset));
        Assert.That(model.IsDirty, Is.EqualTo(!reset));
        service.Verify(x => x.GetLayoutStateAsync(
            It.IsAny<string>(),
            It.IsAny<Customer>(),
            It.IsAny<DashboardLayoutLayer>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Exactly(reset ? 2 : 1));
    }

    private static HomeController CreateController(
        bool personal,
        bool global,
        DashboardLayoutLayer selected,
        Action<Mock<IDashboardService>> configure = null)
    {
        var customer = new Customer { Id = 42 };
        var permissions = new Mock<IPermissionService>();
        permissions.Setup(x => x.AuthorizeAsync(It.IsAny<string>(), null, false))
            .ReturnsAsync((string permission, Customer user, bool descendants) => permission switch
            {
                Permissions.Dashboard.Personalize => personal,
                Permissions.Dashboard.ManageGlobal => global,
                _ => true
            });
        var work = new Mock<IWorkContext>();
        work.SetupGet(x => x.CurrentCustomer).Returns(customer);
        var services = new Mock<ICommonServices> { DefaultValue = DefaultValue.Mock };
        services.SetupGet(x => x.WorkContext).Returns(work.Object);
        services.SetupGet(x => x.Permissions).Returns(permissions.Object);
        var dashboard = new Mock<IDashboardService>();
        dashboard.Setup(x => x.GetSelectedLayer(It.IsAny<string>(), customer)).Returns(selected);
        dashboard.Setup(x => x.GetDescriptors()).Returns(Array.Empty<DashboardWidgetDescriptor>());
        dashboard.Setup(x => x.GetLayoutStateAsync(
            It.IsAny<string>(),
            customer,
            It.IsAny<DashboardLayoutLayer>(),
            false,
            It.IsAny<CancellationToken>()))
            .Returns((string id, Customer user, DashboardLayoutLayer layer, bool reset, CancellationToken token) =>
                ValueTask.FromResult(new DashboardLayoutState { Layout = new DashboardLayout(id), Layer = layer, Source = layer }));
        dashboard.Setup(x => x.PrepareDashboardAsync(
            It.IsAny<DashboardLayout>(),
            customer,
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .Returns((DashboardLayout layout, Customer user, bool editing, CancellationToken token) => ValueTask.FromResult(new DashboardRenderModel
            {
                Layout = layout,
                Context = new DashboardWidgetContext { DashboardId = layout.Id, Customer = user, IsEditMode = editing },
                Widgets = []
            }));
        configure?.Invoke(dashboard);
        var modules = new ModuleManager(null, null, null, Mock.Of<ILocalizationService>(), null, null, null, null);
        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.Action(It.IsAny<Microsoft.AspNetCore.Mvc.Routing.UrlActionContext>()))
            .Returns((Microsoft.AspNetCore.Mvc.Routing.UrlActionContext context) =>
                context.Action == "DashboardPreview" ? "/preview"
                    : new RouteValueDictionary(context.Values).ContainsKey("edit") ? "/edit" : "/cancel");
        return new HomeController(
            Mock.Of<IUserAgentFactory>(),
            Mock.Of<IUserAgent>(),
            dashboard.Object,
            Mock.Of<IModuleCatalog>(),
            modules)
        {
            Services = services.Object,
            Url = url.Object,
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    /// <summary>
    /// Admin viewing and editing explicitly resolve the selected layer and preserve the render context.
    /// </summary>
    [TestCase(false, DashboardLayoutLayer.Global)]
    [TestCase(false, DashboardLayoutLayer.User)]
    [TestCase(true, DashboardLayoutLayer.User)]
    public async Task Rendering_Uses_The_Explicit_Layer(bool editing, DashboardLayoutLayer selected)
    {
        Mock<IDashboardService> service = null;
        var controller = CreateController(true, true, selected, mock => service = mock);
        var result = (ViewResult)await controller.Index(editing);
        var model = (DashboardEditorModel)result.Model;
        Assert.Multiple(() =>
        {
            Assert.That(model.State.Layer, Is.EqualTo(selected));
            Assert.That(model.Dashboard.Layout, Is.SameAs(model.State.Layout));
            Assert.That(model.Dashboard.Context.IsEditMode, Is.EqualTo(editing));
        });

        if (editing)
        {
            var data = JsonNode.Parse(model.EditorJson);
            Assert.That(data["editUrl"].GetValue<string>(), Is.EqualTo("/edit"));
            Assert.That(data["cancelUrl"].GetValue<string>(), Is.EqualTo("/cancel"));
            Assert.That(data["previewUrl"].GetValue<string>(), Is.EqualTo("/preview"));
            Assert.That(data["layer"].GetValue<string>(), Is.EqualTo(selected.ToString()));
            Assert.That(data["layout"]["id"].GetValue<string>(), Is.EqualTo(model.State.Layout.Id));
        }
        else
        {
            Assert.That(model.EditorJson, Is.Null);
        }

        service.Verify(x => x.GetDashboardAsync(It.IsAny<string>(), It.IsAny<Customer>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never());
        service.Verify(x => x.PrepareDashboardAsync(model.State.Layout, It.IsAny<Customer>(), editing, It.IsAny<CancellationToken>()), Times.Once());
        service.Verify(x => x.GetLayoutStateAsync("admin-dashboard", It.IsAny<Customer>(), selected, false, It.IsAny<CancellationToken>()), Times.Once());
    }

    /// <summary>
    /// Normal viewing remains available independently of the two customization permissions.
    /// </summary>
    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task Normal_View_Reports_Independent_Edit_Capabilities(bool personal, bool global)
    {
        var controller = CreateController(personal, global, DashboardLayoutLayer.User);
        var result = (ViewResult)await controller.Index();
        var model = (DashboardEditorModel)result.Model;
        Assert.Multiple(() =>
        {
            Assert.That(model.IsEditing, Is.False);
            Assert.That(model.CanPersonalize, Is.EqualTo(personal));
            Assert.That(model.CanManageGlobal, Is.EqualTo(global));
        });
    }

    /// <summary>
    /// Entering the editor selects an allowed layer when the current view is not editable.
    /// </summary>
    [TestCase(true, false, DashboardLayoutLayer.User, DashboardLayoutLayer.User)]
    [TestCase(true, false, DashboardLayoutLayer.Global, DashboardLayoutLayer.User)]
    [TestCase(false, true, DashboardLayoutLayer.User, DashboardLayoutLayer.Global)]
    [TestCase(false, true, DashboardLayoutLayer.System, DashboardLayoutLayer.Global)]
    [TestCase(true, true, DashboardLayoutLayer.System, DashboardLayoutLayer.User)]
    public async Task Editor_Defaults_To_An_Authorized_Layer(
        bool personal,
        bool global,
        DashboardLayoutLayer selected,
        DashboardLayoutLayer expected)
    {
        var controller = CreateController(personal, global, selected);
        var result = (ViewResult)await controller.Index(true);
        Assert.That(((DashboardEditorModel)result.Model).State.Layer, Is.EqualTo(expected));
    }

    /// <summary>
    /// Viewing the system default does not grant permission to edit any layer.
    /// </summary>
    [TestCase(false, false, DashboardLayoutLayer.User)]
    [TestCase(false, false, DashboardLayoutLayer.Global)]
    [TestCase(true, false, DashboardLayoutLayer.Global)]
    [TestCase(false, true, DashboardLayoutLayer.User)]
    [TestCase(true, true, DashboardLayoutLayer.System)]
    public async Task Forbidden_Edit_Preview_And_Save_Are_Rejected(bool personal, bool global, DashboardLayoutLayer layer)
    {
        var controller = CreateController(personal, global, DashboardLayoutLayer.User);
        Assert.ThrowsAsync<AccessDeniedException>(async () => await controller.Index(true, layer));
        Assert.ThrowsAsync<AccessDeniedException>(async () => await controller.DashboardPreview(layer, "", "", ""));
        Assert.ThrowsAsync<AccessDeniedException>(async () => await controller.DashboardSave(layer, "", false));
        Assert.ThrowsAsync<AccessDeniedException>(async () => await controller.DashboardConfigure("widget", "type", layer, ""));
    }

    /// <summary>
    /// The MVC filter protects every dashboard endpoint when dashboard read access is missing.
    /// </summary>
    [TestCase(nameof(HomeController.Index))]
    [TestCase(nameof(HomeController.DashboardSelect))]
    [TestCase(nameof(HomeController.DashboardPreview))]
    [TestCase(nameof(HomeController.DashboardSave))]
    [TestCase(nameof(HomeController.DashboardConfigure))]
    public void Missing_Read_Permission_Is_Rejected_By_The_Endpoint_Filter(string action)
    {
        var permission = new Mock<IPermissionService>();
        permission.Setup(x => x.AuthorizeAsync(Permissions.Dashboard.Read, null, false)).ReturnsAsync(false);
        permission.Setup(x => x.GetUnauthorizedMessageAsync(Permissions.Dashboard.Read)).ReturnsAsync("");
        using var services = new ServiceCollection().AddSingleton(permission.Object).BuildServiceProvider();
        var attribute = typeof(HomeController).GetMethod(action).GetCustomAttribute<PermissionAttribute>();
        Assert.That(attribute, Is.Not.Null);
        var filter = (IAsyncAuthorizationFilter)attribute.CreateInstance(services);
        var context = new AuthorizationFilterContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>());

        Assert.ThrowsAsync<AccessDeniedException>(async () => await filter.OnAuthorizationAsync(context));
        permission.Verify(x => x.AuthorizeAsync(Permissions.Dashboard.Read, null, false), Times.Once);
    }
}

#nullable disable

/// <summary>
/// Verifies the editor boundary, trusted metadata preservation and script-safe initialization data.
/// </summary>
[TestFixture]
public class DashboardEditorMapperTests
{
    /// <summary>
    /// Server insertion and client editing receive the same named layout constraints.
    /// </summary>
    [Test]
    public void Layout_Rules_Reserve_The_News_Rail_Only_On_Large_Tiers()
    {
        var layout = new AdminDashboardLayoutProvider().GetDefaultLayout();
        var rules = DashboardEditorMapper.GetLayoutRules(layout);

        Assert.Multiple(() =>
        {
            Assert.That(rules.Select(rule => rule.Breakpoint), Is.EqualTo(Enum.GetValues<DashboardBreakpoint>()));
            Assert.That(rules.Where(rule => rule.Breakpoint < DashboardBreakpoint.Md).All(rule => rule.FullWidthWidgets), Is.True);
            Assert.That(rules.Where(rule => rule.Breakpoint >= DashboardBreakpoint.Md).All(rule => !rule.FullWidthWidgets), Is.True);
            Assert.That(rules.Where(rule => rule.Breakpoint < DashboardBreakpoint.Lg).All(rule => rule.ColumnCount == 12 && !rule.HasNewsRail), Is.True);
            Assert.That(rules.Where(rule => rule.Breakpoint >= DashboardBreakpoint.Lg).All(rule => rule.ColumnCount == 10 && rule.HasNewsRail), Is.True);
        });
    }

    /// <summary>
    /// An empty dashboard can place widgets at every named tier without any pixel-based defaults.
    /// </summary>
    [Test]
    public void Empty_Layout_Uses_All_Columns_And_Named_Insertion_Positions()
    {
        var layout = new DashboardLayout("test");
        var positions = DashboardEditorMapper.GetInsertionPositions(layout, new DashboardWidgetSize(4, 2));

        Assert.Multiple(() =>
        {
            Assert.That(DashboardEditorMapper.GetLayoutRules(layout).All(rule => rule.ColumnCount == 12 && !rule.HasNewsRail), Is.True);
            Assert.That(positions.Select(position => position.Breakpoint), Is.EqualTo(Enum.GetValues<DashboardBreakpoint>()));
            Assert.That(positions.All(position => position.Row == 0 && position.Column == 0 && position.Size.RowSpan == 2), Is.True);
            Assert.That(positions.Where(position => position.Breakpoint < DashboardBreakpoint.Md).All(position => position.Size.ColumnSpan == 12), Is.True);
            Assert.That(positions.Where(position => position.Breakpoint >= DashboardBreakpoint.Md).All(position => position.Size.ColumnSpan == 4), Is.True);
        });
    }

    [TestCase("minHeight", "700")]
    [TestCase("stretch", "false")]
    public void Rejects_Pixel_Height_Overrides(string property, string value)
    {
        var json = "{\"widgets\":[{\"id\":\"a\",\"widgetSystemName\":\"Test\",\"positions\":[{\"size\":{\"columnSpan\":4},\"" + property + "\":" + value + "}]}]}";
        Assert.Throws<JsonException>(() => DashboardEditorMapper.ReadChanges(json));
    }

    [Test]
    public void Row_Span_Roundtrips_Through_Editor_Contract()
    {
        var baseline = Baseline();
        var changes = new DashboardLayoutChanges(baseline.Widgets.Select(w => new DashboardWidgetPlacement(
            w.Id,
            w.WidgetSystemName,
            [new DashboardWidgetPosition { Size = new DashboardWidgetSize(4, 2) }])).ToArray());
        var json = JsonSerializer.Serialize(changes, SmartJsonOptions.CamelCased);
        var result = DashboardEditorMapper.ApplyChanges(DashboardEditorMapper.ReadChanges(json), baseline, Mock.Of<IDashboardService>());
        Assert.That(result.Widgets[0].GetPosition(DashboardBreakpoint.Xs).Size.RowSpan, Is.EqualTo(2));
    }


    /// <summary>
    /// Editor placement input remains bounded without restricting provider layouts in the core.
    /// </summary>
    [TestCase(10001, 0, 1)]
    [TestCase(0, 10001, 1)]
    [TestCase(0, 0, 1001)]
    public void Changes_Reject_Positions_Beyond_Editor_Limits(int breakpoint, int row, int rowSpan)
    {
        var baseline = Baseline();
        var instance = baseline.Widgets[0];
        var changes = new DashboardLayoutChanges(
            [new DashboardWidgetPlacement(
                instance.Id,
                instance.WidgetSystemName,
                [new DashboardWidgetPosition { Breakpoint = (DashboardBreakpoint)breakpoint, Row = row, Size = new DashboardWidgetSize(4, rowSpan) }])]);

        Assert.Throws<ArgumentException>(() => DashboardEditorMapper.ApplyChanges(changes, baseline, new Mock<IDashboardService>(MockBehavior.Strict).Object));
        Assert.That(baseline.Widgets[0], Is.SameAs(instance));
    }


    private static DashboardLayout Baseline()
        => new(AdminDashboardLayoutProvider.Id)
        {
            CustomerId = 42,
            Scope = DashboardLayoutScope.User,
            Revision = 7,
            Widgets =
            [
                new DashboardWidgetInstance("existing", StoreReportDashboardWidget.SystemName)
                {
                    Order = 300,
                    SettingsVersion = 4,
                    Settings = new JsonObject { ["serverSetting"] = "preserve-me" },
                    Policy = new DashboardWidgetPolicy { AllowMove = false },
                    Positions = [new DashboardWidgetPosition { Size = new DashboardWidgetSize(4) }]
                },
                new DashboardWidgetInstance("unavailable", "Missing.Module.Widget")
                {
                    Order = 400,
                    Positions = [new DashboardWidgetPosition { Column = 4, Size = new DashboardWidgetSize(4) }]
                }
            ]
        };

    private static DashboardEditorModel EditorModel(DashboardLayout layout)
    {
        var widget = new StoreReportDashboardWidget(Mock.Of<Smartstore.Core.Security.IPermissionService>());
        var context = new DashboardWidgetContext { DashboardId = layout.Id, Customer = new Customer { Id = 42 }, IsEditMode = true };
        return new DashboardEditorModel
        {
            IsEditing = true,
            State = new DashboardLayoutState { Layout = layout, Layer = DashboardLayoutLayer.User, Source = DashboardLayoutLayer.User },
            Dashboard = new DashboardRenderModel
            {
                Layout = layout,
                Context = context,
                Widgets = [new DashboardRenderItem
                {
                    Instance = layout.Widgets[0], Descriptor = widget.Descriptor,
                    Policy = new DashboardWidgetPolicy(), Widget = widget.CreateWidget(context, layout.Widgets[0])
                }]
            },
            Catalog = [widget.Descriptor]
        };
    }

    /// <summary>
    /// Keeps settings, persistence metadata and unavailable widgets off the wire.
    /// </summary>
    [Test]
    public void Start_Data_Contains_Only_Editor_Fields()
    {
        var baseline = Baseline();
        baseline.Widgets.Add(new DashboardWidgetInstance("news-feed", NewsFeedDashboardWidget.SystemName)
        {
            Positions = [new DashboardWidgetPosition { Column = 10, Size = new DashboardWidgetSize(2) }]
        });
        var json = DashboardEditorMapper.CreateJson(EditorModel(baseline), "/edit", "/cancel", "/preview");
        using var document = JsonDocument.Parse(json);
        var widgets = document.RootElement.GetProperty("layout").GetProperty("widgets").EnumerateArray().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(
                document.RootElement.GetProperty("layout").EnumerateObject().Select(x => x.Name),
                Is.EquivalentTo(new[] { "id", "columnCount", "widgets", "rules" }));
            Assert.That(json, Does.Not.Contain("preserve-me").And.Not.Contain("serverSetting").And.Not.Contain("revision").And.Not.Contain("customerId"));
            Assert.That(widgets.Single(x => x.GetProperty("id").GetString() == "existing").GetProperty("allowMove").GetBoolean(), Is.True);
            Assert.That(widgets.Select(x => x.GetProperty("id").GetString()), Is.EqualTo(new[] { "existing" }));
            Assert.That(document.RootElement.GetProperty("layout").GetProperty("rules").EnumerateArray()
                .All(rule => rule.GetProperty("columnCount").GetInt32() == 12 && !rule.GetProperty("hasNewsRail").GetBoolean()), Is.True);
            Assert.That(baseline.Widgets, Has.Count.EqualTo(3));
        });
    }

    /// <summary>
    /// Ensures embedded JSON cannot terminate its application/json script element.
    /// </summary>
    [Test]
    public void Start_Data_Encodes_Script_Closing_Sequences()
    {
        const string url = "</script><script>alert(1)</script>";
        var json = DashboardEditorMapper.CreateJson(EditorModel(Baseline()), url, "/cancel", "/preview");
        Assert.That(json, Does.Not.Contain("</script>"));
        using var document = JsonDocument.Parse(json);
        Assert.That(document.RootElement.GetProperty("editUrl").GetString(), Is.EqualTo(url));
    }

    /// <summary>
    /// Rejects client attempts to send metadata outside the explicit changes contract.
    /// </summary>
    [TestCase("{\"widgets\":[],\"customerId\":42}")]
    [TestCase("{\"widgets\":[{\"id\":\"existing\",\"widgetSystemName\":\"Widget\",\"positions\":[],\"settingsVersion\":2}]}")]
    [TestCase("{\"widgets\":[{\"id\":\"existing\",\"widgetSystemName\":\"Widget\",\"positions\":[],\"allowMove\":true}]}")]
    public void Changes_Reject_Server_Metadata(string json)
        => Assert.Throws<JsonException>(() => DashboardEditorMapper.ReadChanges(json));

    /// <summary>
    /// Preserves existing settings, restrictions, ownership and revision information during placement changes.
    /// </summary>
    [Test]
    public void Applying_Placement_Changes_Preserves_Existing_Settings_And_Server_Metadata()
    {
        var baseline = Baseline();
        var existing = baseline.Widgets[0];
        var unavailable = baseline.Widgets[1];
        var changes = new DashboardLayoutChanges(
        [
            new DashboardWidgetPlacement(
                existing.Id,
                existing.WidgetSystemName,
                [new DashboardWidgetPosition { Row = 2, Size = new DashboardWidgetSize(4) }]),
            new DashboardWidgetPlacement(unavailable.Id, unavailable.WidgetSystemName, unavailable.Positions)
        ]);
        var result = DashboardEditorMapper.ApplyChanges(changes, baseline, new Mock<IDashboardService>(MockBehavior.Strict).Object);
        Assert.Multiple(() =>
        {
            Assert.That(result.CustomerId, Is.EqualTo(42));
            Assert.That(result.Scope, Is.EqualTo(DashboardLayoutScope.User));
            Assert.That(result.Widgets[0].Settings.ToJsonString(), Is.EqualTo(existing.Settings.ToJsonString()));
            Assert.That(result.Widgets[0].SettingsVersion, Is.EqualTo(4));
            Assert.That(result.Widgets[0].Order, Is.EqualTo(300));
            Assert.That(result.Widgets[0].Policy, Is.EqualTo(existing.Policy));
            Assert.That(result.Widgets[0].Positions.Single().Row, Is.EqualTo(2));
            Assert.That(result.Widgets[1], Is.EqualTo(unavailable));
        });
    }

    /// <summary>
    /// Rebuilds newly added, unsaved instances from registered defaults across subsequent preview requests.
    /// </summary>
    [Test]
    public void New_Instances_Use_Server_Defaults()
    {
        var descriptor = new DashboardWidgetDescriptor("New.Widget", "New widget")
        {
            SettingsVersion = 5, DefaultSize = new DashboardWidgetSize(4),
            MinSize = new DashboardWidgetSize(1), MaxSize = new DashboardWidgetSize(12)
        };
        var widget = new Mock<IDashboardWidget>();
        widget.SetupGet(x => x.Descriptor).Returns(descriptor);
        widget.Setup(x => x.CreateDefaultSettings()).Returns(() => new JsonObject { ["default"] = "server" });
        var service = new Mock<IDashboardService>(MockBehavior.Strict);
        service.Setup(x => x.GetWidget("New.Widget")).Returns(widget.Object);
        var changes = new DashboardLayoutChanges(
            [new DashboardWidgetPlacement("widget-new", "New.Widget", [new DashboardWidgetPosition { Size = new DashboardWidgetSize(4) }])]);
        var result = DashboardEditorMapper.ApplyChanges(changes, Baseline(), service.Object);
        Assert.Multiple(() =>
        {
            Assert.That(result.Widgets, Has.Count.EqualTo(1));
            Assert.That(result.Widgets[0].SettingsVersion, Is.EqualTo(5));
            Assert.That(result.Widgets[0].Settings["default"].GetValue<string>(), Is.EqualTo("server"));
            Assert.That(result.Widgets[0].Order, Is.EqualTo(500));
        });
    }

    /// <summary>
    /// Rejects changing an existing instance's type and duplicate instance identities.
    /// </summary>
    [TestCase(true)]
    [TestCase(false)]
    public void Changes_Reject_Identity_Conflicts(bool changeType)
    {
        var baseline = Baseline();
        var instance = baseline.Widgets[0];
        var placement = new DashboardWidgetPlacement(
            instance.Id,
            changeType ? "Another.Widget" : instance.WidgetSystemName,
            instance.Positions);
        var changes = new DashboardLayoutChanges(changeType ? [placement] : [placement, placement]);
        Assert.Throws<ArgumentException>(() => DashboardEditorMapper.ApplyChanges(changes, baseline, new Mock<IDashboardService>(MockBehavior.Strict).Object));
    }
}

[TestFixture]
public class LatestOrdersConfigurationTests
{
    [Test]
    public async Task Base_Uses_The_Injected_Widget_Validator_And_Cancellation_Token()
    {
        var settings = new JsonObject { ["count"] = 12 };
        using var cancellation = new CancellationTokenSource();
        var receivedToken = CancellationToken.None;
        var receivedCount = 0;
        var validator = new LatestOrdersDashboardWidgetValidator();
        validator.RuleFor(x => x.Count).MustAsync((count, token) =>
        {
            receivedToken = token;
            receivedCount = count;
            return Task.FromResult(false);
        }).WithMessage("Widget-specific failure");
        var widget = new LatestOrdersDashboardWidget(Mock.Of<IPermissionService>(), validator);

        var error = Assert.ThrowsAsync<ArgumentException>(async () => await widget.ValidateSettingsAsync(settings, cancellation.Token));
        Assert.That(error.Message, Does.Contain("Widget-specific failure"));
        Assert.That(receivedCount, Is.EqualTo(12));
        Assert.That(receivedToken, Is.EqualTo(cancellation.Token));

        var validWidget = new LatestOrdersDashboardWidget(Mock.Of<IPermissionService>(), new LatestOrdersDashboardWidgetValidator());
        await validWidget.ValidateSettingsAsync(settings, cancellation.Token);
    }

    [Test]
    public async Task Editor_Settings_Survive_Layout_Serialization_And_Reach_Renderer()
    {
        var widget = new LatestOrdersDashboardWidget(Mock.Of<IPermissionService>(), new LatestOrdersDashboardWidgetValidator());
        var service = new Mock<IDashboardService>();
        service.Setup(x => x.GetWidget(LatestOrdersDashboardWidget.SystemName)).Returns(widget);
        var layout = new AdminDashboardLayoutProvider().GetDefaultLayout();
        var instance = layout.Widgets.Single(x => x.WidgetSystemName == LatestOrdersDashboardWidget.SystemName);
        layout.Widgets = [instance];
        var changes = new DashboardLayoutChanges([
            new(instance.Id, instance.WidgetSystemName, instance.Positions, new JsonObject { ["count"] = 12 })]);
        var wire = JsonSerializer.Serialize(changes, SmartJsonOptions.CamelCased);
        DashboardEditorMapper.ApplyChanges(DashboardEditorMapper.ReadChanges(wire), layout, service.Object);
        var stored = JsonSerializer.Serialize(layout, SmartJsonOptions.CamelCased);
        var loaded = JsonSerializer.Deserialize<DashboardLayout>(stored, SmartJsonOptions.CamelCased).Widgets.Single();
        await widget.ValidateSettingsAsync(loaded.Settings);
        var rendered = (ComponentWidget)widget.CreateWidget(new DashboardWidgetContext { DashboardId = layout.Id }, loaded);
        Assert.Multiple(() =>
        {
            Assert.That(loaded.Settings["count"].GetValue<int>(), Is.EqualTo(12));
            Assert.That(rendered.Arguments.GetType().GetProperty("count").GetValue(rendered.Arguments), Is.EqualTo(12));
            Assert.That(loaded.Policy.AllowConfigure, Is.True);
            Assert.That(instance.Settings.Count, Is.Zero);
        });
    }

    [Test]
    public void Editor_Cannot_Change_Settings_Of_Locked_Instance()
    {
        var widget = new LatestOrdersDashboardWidget(Mock.Of<IPermissionService>(), new LatestOrdersDashboardWidgetValidator());
        var service = new Mock<IDashboardService>();
        service.Setup(x => x.GetWidget(LatestOrdersDashboardWidget.SystemName)).Returns(widget);
        var layout = new AdminDashboardLayoutProvider().GetDefaultLayout();
        var instance = layout.Widgets.Single(x => x.WidgetSystemName == LatestOrdersDashboardWidget.SystemName)
            with { Policy = new() { AllowConfigure = false } };
        layout.Widgets = [instance];
        var changes = new DashboardLayoutChanges([
            new(instance.Id, instance.WidgetSystemName, instance.Positions, new JsonObject { ["count"] = 12 })]);
        Assert.Throws<InvalidOperationException>(() => DashboardEditorMapper.ApplyChanges(changes, layout, service.Object));
    }
}

[TestFixture]
public class WidgetAjaxRenderingTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Ajax_Only_Renders_Explicitly_Opted_In_Initialization(bool renderOnAjax)
    {
        var provider = new Mock<IWidgetProvider>();
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
        var helper = new HtmlWidgetTagHelper(provider.Object)
        {
            TargetZone = "scripts",
            RenderOnAjax = renderOnAjax,
            ViewContext = new ViewContext { HttpContext = http }
        };
        var context = new TagHelperContext("script", new TagHelperAttributeList(), new Dictionary<object, object>(), "init");
        var output = new TagHelperOutput("script", new TagHelperAttributeList(), (_, _) =>
            Task.FromResult<TagHelperContent>(new DefaultTagHelperContent().SetHtmlContent("initializeChart();")));

        await helper.ProcessAsync(context, output);

        using var writer = new System.IO.StringWriter();
        output.WriteTo(writer, HtmlEncoder.Default);
        Assert.That(writer.ToString(), Is.EqualTo(renderOnAjax ? "<script>initializeChart();</script>" : ""));
        provider.VerifyNoOtherCalls();
    }
}
