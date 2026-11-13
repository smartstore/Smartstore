using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Moq;
using NUnit.Framework;
using Smartstore.Admin.Infrastructure.Dashboard;
using Smartstore.Admin.Models.Dashboard;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Widgets;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Json;

namespace Smartstore.Web.Tests.Admin.Dashboard;

#nullable enable

/// <summary>
/// Verifies the system fallback, JSON contract and responsive CSS generation of the admin dashboard.
/// </summary>
[TestFixture]
public sealed class DashboardLayoutTests
{
    /// <summary>
    /// Layout customizations cannot unlock or remove the fixed news provider.
    /// </summary>
    [Test]
    public void News_Provider_Restrictions_Cannot_Be_Relaxed_By_A_Layout()
    {
        var provider = new NewsFeedDashboardWidget(Mock.Of<IPermissionService>());
        var policy = DashboardWidgetPolicy.Combine(
            provider.Descriptor.Policy,
            new DashboardWidgetPolicy { AllowMove = true, AllowResize = true, AllowConfigure = true });

        Assert.Multiple(() =>
        {
            Assert.That(policy.IsRequired, Is.True);
            Assert.That(policy.AllowMove, Is.False);
            Assert.That(policy.AllowResize, Is.False);
            Assert.That(policy.AllowConfigure, Is.False);
        });
    }

    /// <summary>
    /// Reuses holes independently at each breakpoint and appends when the requested width does not fit.
    /// </summary>
    [Test]
    public void Insertion_Uses_First_Fitting_Hole_At_Each_Breakpoint()
    {
        var layout = new AdminDashboardLayoutProvider().GetDefaultLayout();
        layout.Widgets.Remove(GetInstance(layout, TopCustomersDashboardWidget.SystemName));
        var positions = DashboardEditorMapper.GetInsertionPositions(layout, new DashboardWidgetSize(6));
        Assert.Multiple(() =>
        {
            Assert.That(positions.Single(x => x.Breakpoint == DashboardBreakpoint.Xs).Row, Is.EqualTo(3));
            Assert.That(positions.Single(x => x.Breakpoint == DashboardBreakpoint.Md).Row, Is.EqualTo(2));
            Assert.That(positions.Single(x => x.Breakpoint == DashboardBreakpoint.Md).Column, Is.EqualTo(6));
            Assert.That(positions.Single(x => x.Breakpoint == DashboardBreakpoint.Lg).Row, Is.EqualTo(2));
            Assert.That(positions.Single(x => x.Breakpoint == DashboardBreakpoint.Lg).Column, Is.EqualTo(4));
            Assert.That(positions.Single(x => x.Breakpoint == DashboardBreakpoint.Xxl).Row, Is.EqualTo(4));
            Assert.That(positions.Where(x => x.Breakpoint >= DashboardBreakpoint.Lg).All(x => x.Column + x.Size.ColumnSpan <= 10), Is.True);
        });
    }

    [TestCase(1, 0, 4)]
    [TestCase(2, 2, 0)]
    public void Insertion_Requires_The_Entire_Rectangle_To_Be_Free(int height, int expectedRow, int expectedColumn)
    {
        var layout = new DashboardLayout("test")
        {
            Widgets =
            [
                new DashboardWidgetInstance("left", "test")
                {
                    Positions = [new DashboardWidgetPosition { Size = new DashboardWidgetSize(4, 2) }]
                },
                new DashboardWidgetInstance("right", "test")
                {
                    Positions = [new DashboardWidgetPosition { Column = 8, Size = new DashboardWidgetSize(4, 2) }]
                },
                new DashboardWidgetInstance("below", "test")
                {
                    Positions = [new DashboardWidgetPosition { Column = 4, Row = 1, Size = new DashboardWidgetSize(4, 1) }]
                }
            ]
        };
        var position = DashboardEditorMapper.GetInsertionPositions(layout, new DashboardWidgetSize(4, height))
            .Single(x => x.Breakpoint == DashboardBreakpoint.Lg);
        Assert.Multiple(() =>
        {
            Assert.That(position.Row, Is.EqualTo(expectedRow));
            Assert.That(position.Column, Is.EqualTo(expectedColumn));
        });
    }

    /// <summary>
    /// Verifies independent personal and shared permissions and the administrator defaults.
    /// </summary>
    [Test]
    public void Dashboard_Permissions_Are_Independent_And_Granted_To_Administrators()
    {
        var provider = new Smartstore.Core.Security.StandardPermissionProvider();
        var names = provider.GetPermissions().Select(x => x.SystemName).ToArray();
        Assert.That(names, Does.Contain(Smartstore.Core.Security.Permissions.Dashboard.Read));
        Assert.That(names, Does.Contain(Smartstore.Core.Security.Permissions.Dashboard.Personalize));
        Assert.That(names, Does.Contain(Smartstore.Core.Security.Permissions.Dashboard.ManageGlobal));
        var defaults = provider.GetDefaultPermissions().Single(x => x.CustomerRoleSystemName == Smartstore.Core.Identity.SystemCustomerRoleNames.Administrators);
        Assert.That(defaults.PermissionRecords.Select(x => x.SystemName), Does.Contain(Smartstore.Core.Security.Permissions.Dashboard.Self));
    }

    /// <summary>
    /// Verifies that generated placement rules are scoped to the concrete dashboard grid identifier.
    /// </summary>
    [Test]
    public void Css_Is_Scoped_To_Dashboard_Grid_Identifier()
    {
        var layout = new AdminDashboardLayoutProvider().GetDefaultLayout();
        var instance = GetInstance(layout, LatestOrdersDashboardWidget.SystemName);
        var dashboardWidget = new LatestOrdersDashboardWidget(Moq.Mock.Of<IPermissionService>(), new LatestOrdersDashboardWidgetValidator());
        var context = new DashboardWidgetContext
        {
            DashboardId = layout.Id,
            Customer = new Customer { Id = 42 },
        };
        var model = new DashboardRenderModel
        {
            Layout = layout,
            Context = context,
            Widgets =
            [
                new DashboardRenderItem
                {
                    Instance = instance,
                    Descriptor = dashboardWidget.Descriptor,
                    Policy = DashboardWidgetPolicy.Combine(dashboardWidget.Descriptor.Policy, instance.Policy),
                    Widget = dashboardWidget.CreateWidget(context, instance)
                }
            ]
        };

        var css = new DashboardCssBuilder().Build(model);

        Assert.Multiple(() =>
        {
            Assert.That(layout.BuildWidgetId(instance.Id), Is.EqualTo("admin-dashboard__latest-orders"));
            Assert.That(
                css,
                Does.Contain("#admin-dashboard__latest-orders { --dashboard-column-xs: 1 / span 12; --dashboard-row-xs: 6 / span 1; }"));
            Assert.That(css, Does.Not.Contain("#admin-dashboard-grid >"));
            Assert.That(css, Does.Contain("--dashboard-column-md:"));
            Assert.That(css, Does.Contain("--dashboard-column-lg:"));
            Assert.That(css, Does.Contain("--dashboard-column-xxl:"));
        });
    }

    private static DashboardWidgetInstance GetInstance(DashboardLayout layout, string systemName)
        => layout.Widgets.Single(x => x.WidgetSystemName == systemName);
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
public class DashboardWidgetConfigurationTests
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
