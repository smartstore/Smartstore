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

namespace Smartstore.Web.Tests.Admin.Dashboard;

#nullable disable

/// <summary>
/// Exercises dashboard view selection, edit authorization and the read-permission filter.
/// </summary>
[TestFixture]
public class HomeControllerDashboardTests
{
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
        service.Verify(x => x.SaveLayoutAsync(It.IsAny<DashboardLayout>(), It.IsAny<Customer>(), It.IsAny<DashboardLayoutLayer>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never());
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

    [TestCase(true, false, DashboardLayoutLayer.User, DashboardLayoutLayer.Global)]
    [TestCase(false, true, DashboardLayoutLayer.Global, DashboardLayoutLayer.User)]
    [TestCase(true, true, DashboardLayoutLayer.User, DashboardLayoutLayer.System)]
    public void Alternative_Save_Target_Requires_Its_Own_Permission(bool personal, bool global,
        DashboardLayoutLayer source, DashboardLayoutLayer target)
    {
        var controller = CreateController(personal, global, source);
        Assert.ThrowsAsync<AccessDeniedException>(async () => await controller.DashboardSave(source, "", false, saveLayer: target));
    }

    [TestCase(DashboardLayoutLayer.User, DashboardLayoutLayer.Global, false)]
    [TestCase(DashboardLayoutLayer.Global, DashboardLayoutLayer.User, false)]
    [TestCase(DashboardLayoutLayer.User, DashboardLayoutLayer.Global, true)]
    public async Task Alternative_Save_Writes_Only_The_Target(DashboardLayoutLayer source, DashboardLayoutLayer target, bool reset)
    {
        Mock<IDashboardService> service = null;
        var controller = CreateController(true, true, source, mock =>
        {
            service = mock;
            mock.Setup(x => x.GetLayoutStateAsync("admin-dashboard", It.IsAny<Customer>(), source, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DashboardLayoutState { Layout = new DashboardLayout("admin-dashboard"), Layer = source, Source = DashboardLayoutLayer.System });
        });
        Assert.That(await controller.DashboardSave(source, "{\"widgets\":[]}", reset, saveLayer: target), Is.TypeOf<RedirectToActionResult>());
        service.Verify(x => x.SaveLayoutAsync(It.IsAny<DashboardLayout>(), It.IsAny<Customer>(), target, false, It.IsAny<CancellationToken>()), Times.Once());
        service.Verify(x => x.SaveLayoutAsync(It.IsAny<DashboardLayout>(), It.IsAny<Customer>(), source, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never());
        service.Verify(x => x.SelectLayerAsync("admin-dashboard", It.IsAny<Customer>(), target, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Test]
    public async Task Personal_Save_Does_Not_Persist_A_View_Selection()
    {
        Mock<IDashboardService> service = null;
        var controller = CreateController(true, false, DashboardLayoutLayer.Global, mock => service = mock);
        Assert.That(await controller.DashboardSave(DashboardLayoutLayer.User, "{\"widgets\":[]}", false), Is.TypeOf<RedirectToActionResult>());
        service.Verify(x => x.SaveLayoutAsync(It.IsAny<DashboardLayout>(), It.IsAny<Customer>(), DashboardLayoutLayer.User, false, It.IsAny<CancellationToken>()), Times.Once());
        service.Verify(x => x.SelectLayerAsync(It.IsAny<string>(), It.IsAny<Customer>(), It.IsAny<DashboardLayoutLayer>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Preview_Reuses_The_Loaded_Draft_And_Reset_Ignores_Submitted_Changes(bool reset)
    {
        Mock<IDashboardService> service = null;
        var state = new DashboardLayoutState
        {
            Layout = new DashboardLayout("admin-dashboard"),
            Layer = DashboardLayoutLayer.User,
            Source = DashboardLayoutLayer.Global
        };
        var controller = CreateController(true, false, DashboardLayoutLayer.User, mock =>
        {
            service = mock;
            mock.Setup(x => x.GetLayoutStateAsync("admin-dashboard", It.IsAny<Customer>(), DashboardLayoutLayer.User, reset, It.IsAny<CancellationToken>()))
                .ReturnsAsync(state);
        });

        var result = (ViewResult)await controller.DashboardPreview(DashboardLayoutLayer.User,
            reset ? "invalid discarded JSON" : "{\"widgets\":[]}", reset, "", "");
        var model = (DashboardEditorModel)result.Model;

        Assert.That(model.State, Is.SameAs(state));
        Assert.That(model.Reset, Is.EqualTo(reset));
        service.Verify(x => x.GetLayoutStateAsync(It.IsAny<string>(), It.IsAny<Customer>(), It.IsAny<DashboardLayoutLayer>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        service.Verify(x => x.ValidateDraftAsync(state.Layout, It.IsAny<Customer>(), DashboardLayoutLayer.User, It.IsAny<CancellationToken>()), reset ? Times.Never() : Times.Once());
        service.Verify(x => x.SaveLayoutAsync(It.IsAny<DashboardLayout>(), It.IsAny<Customer>(), It.IsAny<DashboardLayoutLayer>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Write_Error_Retains_The_Loaded_Draft()
    {
        Mock<IDashboardService> service = null;
        DashboardLayout saved = null;
        var controller = CreateController(true, false, DashboardLayoutLayer.User, mock =>
        {
            service = mock;
            mock.Setup(x => x.SaveLayoutAsync(It.IsAny<DashboardLayout>(), It.IsAny<Customer>(), DashboardLayoutLayer.User, false, It.IsAny<CancellationToken>()))
                .Callback<DashboardLayout, Customer, DashboardLayoutLayer, bool, CancellationToken>((layout, customer, layer, reset, token) => saved = layout)
                .ThrowsAsync(new System.IO.IOException("Test write failure."));
        });

        var result = (ViewResult)await controller.DashboardSave(DashboardLayoutLayer.User, "{\"widgets\":[]}", false);
        var model = (DashboardEditorModel)result.Model;

        Assert.That(model.State.Layout, Is.SameAs(saved));
        Assert.That(model.IsDirty, Is.True);
        service.Verify(x => x.GetLayoutStateAsync(It.IsAny<string>(), It.IsAny<Customer>(), It.IsAny<DashboardLayoutLayer>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static HomeController CreateController(bool personal, bool global, DashboardLayoutLayer selected,
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
        dashboard.Setup(x => x.GetLayoutStateAsync(It.IsAny<string>(), customer, It.IsAny<DashboardLayoutLayer>(), false, It.IsAny<CancellationToken>()))
            .Returns((string id, Customer user, DashboardLayoutLayer layer, bool reset, CancellationToken token) =>
                ValueTask.FromResult(new DashboardLayoutState { Layout = new DashboardLayout(id), Layer = layer, Source = layer }));
        dashboard.Setup(x => x.PrepareDashboardAsync(It.IsAny<DashboardLayout>(), customer, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns((DashboardLayout layout, Customer user, bool editing, CancellationToken token) => ValueTask.FromResult(new DashboardRenderModel
            {
                Layout = layout,
                Context = new DashboardWidgetContext { DashboardId = layout.Id, Customer = user, IsEditMode = editing },
                Widgets = []
            }));
        configure?.Invoke(dashboard);
        return new HomeController(Mock.Of<IUserAgentFactory>(), Mock.Of<IUserAgent>(), dashboard.Object)
        {
            Services = services.Object,
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
    public async Task Editor_Defaults_To_An_Authorized_Layer(bool personal, bool global, DashboardLayoutLayer selected, DashboardLayoutLayer expected)
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
        Assert.ThrowsAsync<AccessDeniedException>(async () => await controller.DashboardPreview(layer, "", false, "", ""));
        Assert.ThrowsAsync<AccessDeniedException>(async () => await controller.DashboardSave(layer, "", false));
    }

    /// <summary>
    /// The MVC filter protects every dashboard endpoint when dashboard read access is missing.
    /// </summary>
    [TestCase(nameof(HomeController.Index))]
    [TestCase(nameof(HomeController.DashboardSelect))]
    [TestCase(nameof(HomeController.DashboardPreview))]
    [TestCase(nameof(HomeController.DashboardSave))]
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
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()), new List<IFilterMetadata>());

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
            w.Id, w.WidgetSystemName, [new DashboardWidgetPosition { Size = new DashboardWidgetSize(4, 2) }])).ToArray());
        var json = JsonSerializer.Serialize(changes, SmartJsonOptions.CamelCased);
        var result = DashboardEditorMapper.ApplyChanges(DashboardEditorMapper.ReadChanges(json), baseline, Mock.Of<IDashboardService>());
        Assert.That(result.Widgets[0].GetPosition(DashboardBreakpoint.Xs).Size.RowSpan, Is.EqualTo(2));
    }

    [Test]
    public void Template_Preserves_Target_Settings_And_Locked_News_Placements()
    {
        var source = new AdminDashboardLayoutProvider().GetDefaultLayout();
        var target = new AdminDashboardLayoutProvider().GetDefaultLayout();
        var registrations = target.Widgets.Single(x => x.Id == "registered-customers");
        var news = target.Widgets.Single(x => x.Id == "news-feed");
        target.Widgets[target.Widgets.IndexOf(registrations)] = registrations with
        {
            Settings = new JsonObject { ["existingSetting"] = "keep" },
            Positions = [new DashboardWidgetPosition { Column = 3, Row = 60, Size = new DashboardWidgetSize(3) }]
        };
        target.Widgets[target.Widgets.IndexOf(news)] = news with
        {
            Positions = [new DashboardWidgetPosition { Column = 10, Row = 0, Size = new DashboardWidgetSize(2) }]
        };
        var service = new Mock<IDashboardService>();
        service.Setup(x => x.GetDescriptors()).Returns([
            new RegisteredCustomersDashboardWidget(Mock.Of<Smartstore.Core.Security.IPermissionService>()).Descriptor,
            new NewsFeedDashboardWidget(Mock.Of<Smartstore.Core.Security.IPermissionService>()).Descriptor]);

        var draft = DashboardEditorMapper.ApplyTemplate(source, target, service.Object.GetDescriptors(), service.Object);
        var copied = draft.Widgets.Single(x => x.Id == registrations.Id);
        Assert.Multiple(() =>
        {
            Assert.That(copied.GetPosition(DashboardBreakpoint.Xxl).Size.ColumnSpan, Is.EqualTo(7));
            Assert.That(copied.GetPosition(DashboardBreakpoint.Md).Size.ColumnSpan, Is.EqualTo(12));
            Assert.That(copied.Settings["existingSetting"].GetValue<string>(), Is.EqualTo("keep"));
            Assert.That(draft.Widgets.Single(x => x.Id == news.Id).GetPosition(DashboardBreakpoint.Xs).Column, Is.EqualTo(10));
            Assert.That(draft.Widgets.Single(x => x.Id == news.Id).GetPosition(DashboardBreakpoint.Xs).Size.ColumnSpan, Is.EqualTo(2));
            Assert.That(source.Widgets.Single(x => x.Id == news.Id).GetPosition(DashboardBreakpoint.Xs).Column, Is.Zero);
        });
    }

    /// <summary>
    /// Template adoption preserves unavailable target instances and never adds unavailable source instances.
    /// </summary>
    [TestCase(true, true)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    public void Template_Preserves_Unavailable_Target_Widgets(bool sourceContainsHidden, bool targetContainsHidden)
    {
        var descriptor = new StoreReportDashboardWidget(Mock.Of<Smartstore.Core.Security.IPermissionService>()).Descriptor;
        var source = new DashboardLayout(AdminDashboardLayoutProvider.Id);
        var target = new DashboardLayout(AdminDashboardLayoutProvider.Id);
        source.Widgets.Add(new DashboardWidgetInstance("visible", descriptor.SystemName)
        {
            Positions = [new DashboardWidgetPosition { Column = 2, Row = 10, Size = new DashboardWidgetSize(6) }]
        });
        target.Widgets.Add(new DashboardWidgetInstance("visible", descriptor.SystemName)
        {
            Positions = [new DashboardWidgetPosition { Column = 0, Row = 0, Size = new DashboardWidgetSize(4) }]
        });
        var hidden = new DashboardWidgetInstance("hidden", BestsellersDashboardWidget.SystemName)
        {
            Order = 700,
            Settings = new JsonObject { ["keep"] = true },
            Positions =
            [
                new DashboardWidgetPosition { Column = 0, Row = 30, Size = new DashboardWidgetSize(4, 20) },
                new DashboardWidgetPosition { Breakpoint = DashboardBreakpoint.Lg, Column = 8, Row = 10, Size = new DashboardWidgetSize(4, 30) }
            ]
        };
        if (sourceContainsHidden)
            source.Widgets.Add(hidden with { Positions = [new DashboardWidgetPosition { Size = new DashboardWidgetSize(12) }] });
        if (targetContainsHidden)
            target.Widgets.Add(hidden);
        var sourceBefore = JsonSerializer.Serialize(source);
        var hiddenBefore = JsonSerializer.Serialize(hidden);
        var service = new Mock<IDashboardService>(MockBehavior.Strict);

        var draft = DashboardEditorMapper.ApplyTemplate(source, target, [descriptor], service.Object);

        Assert.Multiple(() =>
        {
            Assert.That(draft.Widgets.Single(x => x.Id == "visible").Positions, Is.EqualTo(source.Widgets[0].Positions));
            Assert.That(draft.Widgets.Any(x => x.Id == "hidden"), Is.EqualTo(targetContainsHidden));
            if (targetContainsHidden)
                Assert.That(JsonSerializer.Serialize(draft.Widgets.Single(x => x.Id == "hidden")), Is.EqualTo(hiddenBefore));
            Assert.That(JsonSerializer.Serialize(source), Is.EqualTo(sourceBefore));
        });
        service.VerifyNoOtherCalls();
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
            [new DashboardWidgetPlacement(instance.Id, instance.WidgetSystemName,
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
    /// Keeps settings and persistence metadata off the wire while retaining unavailable placements without reserving grid space.
    /// </summary>
    [Test]
    public void Start_Data_Contains_Only_Editor_Fields()
    {
        var json = DashboardEditorMapper.CreateJson(EditorModel(Baseline()), "/edit", "/cancel", "/preview");
        using var document = JsonDocument.Parse(json);
        var widgets = document.RootElement.GetProperty("layout").GetProperty("widgets").EnumerateArray().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("layout").EnumerateObject().Select(x => x.Name),
                Is.EquivalentTo(new[] { "id", "columnCount", "widgets", "rules" }));
            Assert.That(json, Does.Not.Contain("preserve-me").And.Not.Contain("serverSetting").And.Not.Contain("revision").And.Not.Contain("customerId"));
            Assert.That(widgets.Single(x => x.GetProperty("id").GetString() == "existing").GetProperty("allowMove").GetBoolean(), Is.True);
            Assert.That(widgets.Single(x => x.GetProperty("id").GetString() == "unavailable").GetProperty("allowMove").GetBoolean(), Is.False);
            Assert.That(widgets.Single(x => x.GetProperty("id").GetString() == "unavailable").GetProperty("positions").GetArrayLength(), Is.EqualTo(1));
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
    [TestCase("{\"widgets\":[{\"id\":\"existing\",\"widgetSystemName\":\"Widget\",\"positions\":[],\"settings\":{}}]}")]
    [TestCase("{\"widgets\":[{\"id\":\"existing\",\"widgetSystemName\":\"Widget\",\"positions\":[],\"allowMove\":true}]}")]
    public void Changes_Reject_Server_Metadata(string json)
        => Assert.Throws<JsonException>(() => DashboardEditorMapper.ReadChanges(json));

    /// <summary>
    /// Preserves existing settings, restrictions, ownership and revision information during placement changes.
    /// </summary>
    [Test]
    public void Applying_Changes_Preserves_Server_Metadata_And_Unavailable_Instances()
    {
        var baseline = Baseline();
        var existing = baseline.Widgets[0];
        var unavailable = baseline.Widgets[1];
        var changes = new DashboardLayoutChanges(
        [
            new DashboardWidgetPlacement(existing.Id, existing.WidgetSystemName,
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
        var placement = new DashboardWidgetPlacement(instance.Id,
            changeType ? "Another.Widget" : instance.WidgetSystemName, instance.Positions);
        var changes = new DashboardLayoutChanges(changeType ? [placement] : [placement, placement]);
        Assert.Throws<ArgumentException>(() => DashboardEditorMapper.ApplyChanges(changes, baseline, new Mock<IDashboardService>(MockBehavior.Strict).Object));
    }
}
