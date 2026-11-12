using System.Linq;
using Moq;
using NUnit.Framework;
using Smartstore.Admin.Infrastructure.Dashboard;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Widgets.Dashboard;

namespace Smartstore.Web.Tests.Admin.Dashboard;

#nullable enable

/// <summary>
/// Verifies the system fallback, JSON contract and responsive CSS generation of the admin dashboard.
/// </summary>
[TestFixture]
public sealed class AdminDashboardTests
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
