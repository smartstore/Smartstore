#nullable enable

using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using Smartstore.Admin.Infrastructure.Dashboard;
using Smartstore.Core.Identity;
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
    /// Verifies that the system fallback reproduces the legacy widget placements at every breakpoint.
    /// </summary>
    [Test]
    public void System_Default_Matches_Legacy_Widget_Placements()
    {
        var layout = new AdminDashboardLayoutProvider().GetDefaultLayout();
        var incompleteOrders = GetInstance(layout, IncompleteOrdersDashboardWidget.SystemName);
        var orders = GetInstance(layout, OrdersDashboardWidget.SystemName);
        var bestsellers = GetInstance(layout, BestsellersDashboardWidget.SystemName);
        var topCustomers = GetInstance(layout, TopCustomersDashboardWidget.SystemName);
        var registeredCustomers = GetInstance(layout, RegisteredCustomersDashboardWidget.SystemName);
        var latestOrders = GetInstance(layout, LatestOrdersDashboardWidget.SystemName);
        var newsFeed = GetInstance(layout, NewsFeedDashboardWidget.SystemName);

        Assert.Multiple(() =>
        {
            Assert.That(layout.GridTemplateColumns, Is.EqualTo("repeat(10, 1fr) repeat(2, 130px)"));
            Assert.That(layout.Widgets, Has.Count.EqualTo(7));

            AssertPosition(incompleteOrders.GetPosition(0), 0, 0, 12);
            AssertPosition(incompleteOrders.GetPosition(768), 0, 0, 12);
            AssertPosition(incompleteOrders.GetPosition(992), 0, 0, 10);
            AssertPosition(incompleteOrders.GetPosition(1600), 0, 0, 10);

            AssertPosition(orders.GetPosition(0), 0, 1, 12);
            AssertPosition(orders.GetPosition(768), 0, 1, 12);
            AssertPosition(orders.GetPosition(992), 0, 1, 10);
            AssertPosition(orders.GetPosition(1600), 0, 1, 7);

            AssertPosition(bestsellers.GetPosition(0), 0, 2, 12);
            AssertPosition(bestsellers.GetPosition(768), 0, 2, 6);
            AssertPosition(bestsellers.GetPosition(992), 0, 2, 4);
            AssertPosition(bestsellers.GetPosition(1600), 7, 1, 3);

            AssertPosition(topCustomers.GetPosition(0), 0, 3, 12);
            AssertPosition(topCustomers.GetPosition(768), 6, 2, 6);
            AssertPosition(topCustomers.GetPosition(992), 4, 2, 6);
            AssertPosition(topCustomers.GetPosition(1600), 0, 2, 3);

            AssertPosition(registeredCustomers.GetPosition(0), 0, 4, 12);
            AssertPosition(registeredCustomers.GetPosition(768), 0, 3, 12);
            AssertPosition(registeredCustomers.GetPosition(992), 0, 3, 10);
            AssertPosition(registeredCustomers.GetPosition(1600), 3, 2, 7);

            AssertPosition(latestOrders.GetPosition(0), 0, 5, 12);
            AssertPosition(latestOrders.GetPosition(768), 0, 4, 12);
            AssertPosition(latestOrders.GetPosition(992), 0, 4, 10);
            AssertPosition(latestOrders.GetPosition(1600), 0, 3, 6);

            AssertPosition(newsFeed.GetPosition(0), 0, 7, 12);
            AssertPosition(newsFeed.GetPosition(768), 0, 6, 12);
            AssertPosition(newsFeed.GetPosition(992), 10, 0, 2, 6);
            AssertPosition(newsFeed.GetPosition(1600), 10, 0, 2, 4);
        });
    }

    /// <summary>
    /// Verifies that layout JSON preserves scopes, widget identities and responsive positions.
    /// </summary>
    [Test]
    public void Json_Contract_Roundtrips_System_Default()
    {
        var source = new AdminDashboardLayoutProvider().GetDefaultLayout();

        var json = JsonSerializer.Serialize(source, SmartJsonOptions.CamelCased);
        var result = JsonSerializer.Deserialize<DashboardLayout>(json, SmartJsonOptions.CamelCased)!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(AdminDashboardLayoutProvider.Id));
            Assert.That(result.Scope, Is.EqualTo(DashboardLayoutScope.Global));
            Assert.That(result.Widgets, Has.Count.EqualTo(7));
            Assert.That(
                result.Widgets.Select(x => x.WidgetSystemName),
                Does.Contain(LatestOrdersDashboardWidget.SystemName));
            Assert.That(
                result.Widgets.Single(x => x.WidgetSystemName == LatestOrdersDashboardWidget.SystemName).Positions,
                Has.Count.EqualTo(4));
        });
    }

    /// <summary>
    /// Verifies that generated placement rules are scoped to the concrete dashboard grid identifier.
    /// </summary>
    [Test]
    public void Css_Is_Scoped_To_Dashboard_Grid_Identifier()
    {
        var layout = new AdminDashboardLayoutProvider().GetDefaultLayout();
        var instance = GetInstance(layout, LatestOrdersDashboardWidget.SystemName);
        var dashboardWidget = new LatestOrdersDashboardWidget();
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
        int columnSpan,
        int rowSpan = 1)
    {
        Assert.That(position.Column, Is.EqualTo(column));
        Assert.That(position.Row, Is.EqualTo(row));
        Assert.That(position.Size.ColumnSpan, Is.EqualTo(columnSpan));
        Assert.That(position.Size.RowSpan, Is.EqualTo(rowSpan));
    }

    private static DashboardWidgetInstance GetInstance(DashboardLayout layout, string systemName)
        => layout.Widgets.Single(x => x.WidgetSystemName == systemName);
}
