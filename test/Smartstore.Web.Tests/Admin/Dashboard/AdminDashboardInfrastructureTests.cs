#nullable enable

using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using Smartstore.Admin.Infrastructure.Dashboard;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Json;

namespace Smartstore.Web.Tests.Admin.Dashboard;

/// <summary>
/// Verifies the system fallback, JSON contract and responsive CSS generation of the admin dashboard.
/// </summary>
[TestFixture]
public sealed class AdminDashboardInfrastructureTests
{
    /// <summary>
    /// Verifies that the system fallback reproduces the legacy latest-orders placement at every breakpoint.
    /// </summary>
    [Test]
    public void System_Default_Matches_Legacy_Latest_Orders_Placement()
    {
        var layout = new MainDashboardLayoutProvider().GetDefaultLayout();
        var instance = layout.Widgets.Single();

        Assert.Multiple(() =>
        {
            Assert.That(layout.GridTemplateColumns, Is.EqualTo("repeat(10, 1fr) repeat(2, 130px)"));
            AssertPosition(instance.GetPosition(0), 0, 5, 12);
            AssertPosition(instance.GetPosition(768), 0, 4, 12);
            AssertPosition(instance.GetPosition(992), 0, 4, 10);
            AssertPosition(instance.GetPosition(1600), 0, 3, 6);
        });
    }

    /// <summary>
    /// Verifies that layout JSON preserves scopes, widget identities and responsive positions.
    /// </summary>
    [Test]
    public void Json_Contract_Roundtrips_System_Default()
    {
        var source = new MainDashboardLayoutProvider().GetDefaultLayout();

        var json = JsonSerializer.Serialize(source, SmartJsonOptions.CamelCased);
        var result = JsonSerializer.Deserialize<DashboardLayout>(json, SmartJsonOptions.CamelCased)!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(MainDashboardLayoutProvider.Id));
            Assert.That(result.Scope, Is.EqualTo(DashboardLayoutScope.Global));
            Assert.That(result.Widgets, Has.Count.EqualTo(1));
            Assert.That(result.Widgets[0].WidgetSystemName, Is.EqualTo(LatestOrdersDashboardWidget.SystemName));
            Assert.That(result.Widgets[0].Positions, Has.Count.EqualTo(4));
        });
    }

    /// <summary>
    /// Verifies that generated placement rules are scoped to the concrete dashboard grid identifier.
    /// </summary>
    [Test]
    public void Css_Is_Scoped_To_Dashboard_Grid_Identifier()
    {
        var layout = new MainDashboardLayoutProvider().GetDefaultLayout();
        var instance = layout.Widgets.Single();
        var dashboardWidget = new LatestOrdersDashboardWidget();
        var context = new DashboardWidgetContext
        {
            DashboardId = layout.Id,
            CustomerId = 42
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
            Assert.That(css, Does.Contain("#admin-dashboard-grid > #latest-orders"));
            Assert.That(css, Does.Contain("grid-column: 1 / span 12;"));
            Assert.That(css, Does.Contain("grid-row: 6 / span 1;"));
            Assert.That(css, Does.Contain("@media screen and (min-width: 768px)"));
            Assert.That(css, Does.Contain("@media screen and (min-width: 992px)"));
            Assert.That(css, Does.Contain("@media screen and (min-width: 1600px)"));
        });
    }

    /// <summary>
    /// Asserts the relevant coordinates and size of a responsive widget position.
    /// </summary>
    /// <param name="position">The position to verify.</param>
    /// <param name="column">The expected zero-based start column.</param>
    /// <param name="row">The expected zero-based start row.</param>
    /// <param name="columnSpan">The expected number of occupied columns.</param>
    private static void AssertPosition(
        DashboardWidgetPosition position,
        int column,
        int row,
        int columnSpan)
    {
        Assert.That(position.Column, Is.EqualTo(column));
        Assert.That(position.Row, Is.EqualTo(row));
        Assert.That(position.Size.ColumnSpan, Is.EqualTo(columnSpan));
        Assert.That(position.Size.RowSpan, Is.EqualTo(1));
    }
}
