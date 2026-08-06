#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using NUnit.Framework;
using Smartstore.Core.Widgets;
using Smartstore.Core.Widgets.Dashboard;

namespace Smartstore.Core.Tests.Platform.Widgets;

/// <summary>
/// Verifies dashboard widget registration, layout resolution and render-model preparation.
/// </summary>
[TestFixture]
public sealed class DashboardWidgetServiceTests
{
    /// <summary>
    /// Verifies that a higher-priority user layout overrides a global layout.
    /// </summary>
    [Test]
    public async Task Resolves_Highest_Ordered_Layout()
    {
        var globalLayout = CreateLayout(DashboardLayoutScope.Global);
        var userLayout = CreateLayout(DashboardLayoutScope.User, 42);
        var service = new DashboardWidgetService(
            [],
            [
                new TestLayoutProvider(0, globalLayout),
                new TestLayoutProvider(1000, userLayout)
            ]);

        var result = await service.GetEffectiveLayoutAsync("admin-dashboard", 42);

        Assert.That(result, Is.SameAs(userLayout));
    }

    /// <summary>
    /// Verifies settings migration, source immutability and effective policy calculation.
    /// </summary>
    [Test]
    public async Task Creates_Render_Model_And_Processes_Settings_And_Policy()
    {
        var widget = new TestDashboardWidget();
        var instance = CreateInstance() with
        {
            SettingsVersion = 1,
            Settings = new JsonObject { ["value"] = 1 },
            Policy = new DashboardWidgetPolicy
            {
                IsRequired = true,
                AllowResize = true
            }
        };

        var layout = CreateLayout(DashboardLayoutScope.Global, widgets: [instance]);
        var service = new DashboardWidgetService(
            [widget],
            [new TestLayoutProvider(0, layout)]);

        var result = await service.GetDashboardAsync("admin-dashboard", 42, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Context.DashboardId, Is.EqualTo("admin-dashboard"));
            Assert.That(result.Context.CustomerId, Is.EqualTo(42));
            Assert.That(result.Context.IsEditMode, Is.True);
            Assert.That(result.Widgets, Has.Count.EqualTo(1));

            var item = result.Widgets[0];
            Assert.That(item.Widget, Is.TypeOf<HtmlWidget>());
            Assert.That(item.Instance.SettingsVersion, Is.EqualTo(2));
            Assert.That(item.Instance.Settings["migrated"]!.GetValue<bool>(), Is.True);
            Assert.That(item.Policy.IsRequired, Is.True);
            Assert.That(item.Policy.AllowResize, Is.False);

            // Runtime migration must not mutate the source layout.
            Assert.That(instance.SettingsVersion, Is.EqualTo(1));
            Assert.That(instance.Settings.ContainsKey("migrated"), Is.False);
        });
    }

    /// <summary>
    /// Verifies that a widget type declared as a singleton cannot occur more than once in a layout.
    /// </summary>
    [Test]
    public void Rejects_Multiple_Instances_Of_Singleton_Widget()
    {
        var widget = new TestDashboardWidget();
        var layout = CreateLayout(
            DashboardLayoutScope.Global,
            widgets:
            [
                CreateInstance("first"),
                CreateInstance("second")
            ]);

        var service = new DashboardWidgetService(
            [widget],
            [new TestLayoutProvider(0, layout)]);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.GetDashboardAsync("admin-dashboard", 0));
    }

    /// <summary>
    /// Verifies that identifiers which cannot safely be used in CSS selectors are rejected.
    /// </summary>
    [Test]
    public void Rejects_Unsafe_Dashboard_And_Instance_Identifiers()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => new DashboardLayout("admin.dashboard"));
            Assert.Throws<ArgumentException>(() => new DashboardWidgetInstance("42-widget", TestDashboardWidget.SystemName));
        });
    }

    /// <summary>
    /// Creates a valid dashboard layout for a test scenario.
    /// </summary>
    /// <param name="scope">The scope assigned to the layout.</param>
    /// <param name="customerId">The customer assigned to a user-scoped layout.</param>
    /// <param name="widgets">The widget instances contained in the layout.</param>
    /// <returns>The test dashboard layout.</returns>
    private static DashboardLayout CreateLayout(
        DashboardLayoutScope scope,
        int customerId = 0,
        IReadOnlyList<DashboardWidgetInstance>? widgets = null)
    {
        return new DashboardLayout("admin-dashboard")
        {
            Scope = scope,
            CustomerId = customerId,
            Widgets = widgets ?? []
        };
    }

    /// <summary>
    /// Creates a valid instance of the test dashboard widget.
    /// </summary>
    /// <param name="id">The dashboard-unique instance identifier.</param>
    /// <returns>The test widget instance.</returns>
    private static DashboardWidgetInstance CreateInstance(string id = "test-widget")
    {
        return new DashboardWidgetInstance(id, TestDashboardWidget.SystemName)
        {
            Positions =
            [
                new DashboardWidgetPosition
                {
                    Size = new DashboardWidgetSize(4)
                }
            ]
        };
    }
}
