using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Smartstore.Admin.Components;
using Smartstore.Admin.Infrastructure.Dashboard;
using Smartstore.Admin.Models.Customers;
using Smartstore.Caching;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Messaging.Events;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Core.Widgets;
using Smartstore.Core;
using Smartstore.Data.Hooks;
using Smartstore.Events;
using Smartstore.Json;

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
        var policy = DashboardWidgetPolicy.Combine(provider.Descriptor.Policy,
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
    /// Rejects direct invocation of the payment report before any data or payment provider is accessed.
    /// </summary>
    [Test]
    public async Task Payment_Method_Report_Requires_Widget_Permission()
    {
        var permissions = new Mock<IPermissionService>(MockBehavior.Strict);
        permissions.Setup(x => x.AuthorizeAsync(Permissions.Dashboard.Widgets.Sales.PaymentMethods, null, false)).ReturnsAsync(false);
        var services = new Mock<ICommonServices>(MockBehavior.Strict);
        services.SetupGet(x => x.Permissions).Returns(permissions.Object);
        using var provider = new ServiceCollection().AddSingleton(services.Object).BuildServiceProvider();
        var component = new DashboardPaymentMethodsViewComponent(null!, null!, null!)
        {
            ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { RequestServices = provider } }
            }
        };

        var result = await component.InvokeAsync();

        Assert.That(result, Is.TypeOf<ContentViewComponentResult>());
        Assert.That(((ContentViewComponentResult)result).Content, Is.Empty);
        permissions.Verify(x => x.AuthorizeAsync(Permissions.Dashboard.Widgets.Sales.PaymentMethods, null, false), Times.Once);
        services.VerifyGet(x => x.Permissions, Times.Once);
        services.VerifyNoOtherCalls();
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
            Assert.That(positions.Single(x => x.MinViewportWidth == 0).Row, Is.EqualTo(3));
            Assert.That(positions.Single(x => x.MinViewportWidth == 768).Row, Is.EqualTo(2));
            Assert.That(positions.Single(x => x.MinViewportWidth == 768).Column, Is.EqualTo(6));
            Assert.That(positions.Single(x => x.MinViewportWidth == 992).Row, Is.EqualTo(2));
            Assert.That(positions.Single(x => x.MinViewportWidth == 992).Column, Is.EqualTo(4));
            Assert.That(positions.Single(x => x.MinViewportWidth == 1600).Row, Is.EqualTo(4));
            Assert.That(positions.Where(x => x.MinViewportWidth >= 992).All(x => x.Column + x.Size.ColumnSpan <= 10), Is.True);
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
            .Single(x => x.MinViewportWidth == 992);
        Assert.Multiple(() =>
        {
            Assert.That(position.Row, Is.EqualTo(expectedRow));
            Assert.That(position.Column, Is.EqualTo(expectedColumn));
        });
    }

    /// <summary>
    /// Verifies shared authorization for all data widgets, including denied access.
    /// </summary>
    [TestCase(true)]
    [TestCase(false)]
    public async System.Threading.Tasks.Task Widget_Availability_Uses_Data_Permission(bool allowed)
    {
        var customer = new Customer { Id = 42 };
        var permissions = new Mock<Smartstore.Core.Security.IPermissionService>(MockBehavior.Strict);
        permissions.Setup(x => x.AuthorizeAsync(It.IsAny<string>(), customer, false)).ReturnsAsync(allowed);
        permissions.Setup(x => x.AuthorizeAsync(Smartstore.Core.Security.Permissions.Customer.Read, customer, false)).ReturnsAsync(allowed);
        permissions.Setup(x => x.AuthorizeAsync(Smartstore.Core.Security.Permissions.System.Message.Read, customer, false)).ReturnsAsync(allowed);
        (IDashboardWidget Widget, string Permission)[] widgets =
        [
            (new BestsellersDashboardWidget(permissions.Object), Smartstore.Core.Security.Permissions.Dashboard.Widgets.Sales.Bestsellers),
            (new IncompleteOrdersDashboardWidget(permissions.Object), Smartstore.Core.Security.Permissions.Dashboard.Widgets.Sales.IncompleteOrders),
            (new OrdersDashboardWidget(permissions.Object), Smartstore.Core.Security.Permissions.Dashboard.Widgets.Sales.Orders),
            (new LatestOrdersDashboardWidget(permissions.Object), Smartstore.Core.Security.Permissions.Dashboard.Widgets.Sales.LatestOrders),
            (new PaymentMethodsDashboardWidget(permissions.Object), Smartstore.Core.Security.Permissions.Dashboard.Widgets.Sales.PaymentMethods),
            (new CookieConsentsDashboardWidget(permissions.Object), Smartstore.Core.Security.Permissions.Dashboard.Widgets.Customers.CookieConsents),
            (new ContactRequestsDashboardWidget(permissions.Object), Smartstore.Core.Security.Permissions.Dashboard.Widgets.Customers.ContactRequests),
            (new TopCustomersDashboardWidget(permissions.Object), Smartstore.Core.Security.Permissions.Dashboard.Widgets.Customers.TopCustomers),
            (new RegisteredCustomersDashboardWidget(permissions.Object), Smartstore.Core.Security.Permissions.Dashboard.Widgets.Customers.RegisteredCustomers)
        ];
        foreach (var (widget, permission) in widgets)
        {
            permissions.Invocations.Clear();
            var result = await widget.IsAvailableAsync(new DashboardWidgetContext { DashboardId = "admin-dashboard", Customer = customer });
            Assert.That(result, Is.EqualTo(allowed), widget.GetType().Name);
            permissions.Verify(x => x.AuthorizeAsync(permission, customer, false), Times.Once);
            permissions.VerifyNoOtherCalls();
        }
    }

    /// <summary>
    /// Keeps widgets without data permission requirements available through the parameterless base constructor.
    /// </summary>
    [Test]
    public async System.Threading.Tasks.Task Widgets_Without_Granted_Permissions_Are_Unavailable()
    {
        var context = new DashboardWidgetContext { DashboardId = "admin-dashboard", Customer = new Customer { Id = 42 } };
        Assert.That(await new NewsFeedDashboardWidget(Mock.Of<IPermissionService>()).IsAvailableAsync(context), Is.False);
        Assert.That(await new StoreReportDashboardWidget(Mock.Of<IPermissionService>()).IsAvailableAsync(context), Is.False);
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
    /// Verifies that the system fallback provides equal-width columns and raster coordinates at every breakpoint.
    /// </summary>
    [Test]
    public void System_Default_Uses_Raster_Coordinates()
    {
        var layout = new AdminDashboardLayoutProvider().GetDefaultLayout();
        var incompleteOrders = GetInstance(layout, IncompleteOrdersDashboardWidget.SystemName);
        var orders = GetInstance(layout, OrdersDashboardWidget.SystemName);
        var bestsellers = GetInstance(layout, BestsellersDashboardWidget.SystemName);
        var topCustomers = GetInstance(layout, TopCustomersDashboardWidget.SystemName);
        var registeredCustomers = GetInstance(layout, RegisteredCustomersDashboardWidget.SystemName);
        var latestOrders = GetInstance(layout, LatestOrdersDashboardWidget.SystemName);
        var storeReport = GetInstance(layout, StoreReportDashboardWidget.SystemName);
        var newsFeed = GetInstance(layout, NewsFeedDashboardWidget.SystemName);

        Assert.Multiple(() =>
        {
            Assert.That(layout.GridTemplateColumns, Is.EqualTo("repeat(10, minmax(0, 1fr)) repeat(2, 130px)"));
            Assert.That(layout.Widgets, Has.Count.EqualTo(8));

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

            AssertPosition(storeReport.GetPosition(0), 0, 6, 12);
            AssertPosition(storeReport.GetPosition(768), 0, 5, 12);
            AssertPosition(storeReport.GetPosition(992), 0, 5, 10);
            AssertPosition(storeReport.GetPosition(1600), 6, 3, 4);

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
            Assert.That(result.Widgets, Has.Count.EqualTo(8));
            Assert.That(
                result.Widgets.Select(x => x.WidgetSystemName),
                Does.Contain(LatestOrdersDashboardWidget.SystemName));
            Assert.That(
                result.Widgets.Select(x => x.WidgetSystemName),
                Does.Contain(StoreReportDashboardWidget.SystemName));
            Assert.That(
                result.Widgets.Single(x => x.WidgetSystemName == LatestOrdersDashboardWidget.SystemName).Positions,
                Has.Count.EqualTo(4));
        });
    }

    /// <summary>
    /// Verifies that the store report is rendered through the partial-view widget infrastructure.
    /// </summary>
    [Test]
    public void Store_Report_Creates_Partial_View_Widget()
    {
        var layout = new AdminDashboardLayoutProvider().GetDefaultLayout();
        var instance = GetInstance(layout, StoreReportDashboardWidget.SystemName);
        var dashboardWidget = new StoreReportDashboardWidget(Mock.Of<IPermissionService>());
        var context = new DashboardWidgetContext
        {
            DashboardId = layout.Id,
            Customer = new Customer { Id = 42 }
        };

        var widget = dashboardWidget.CreateWidget(context, instance);

        Assert.Multiple(() =>
        {
            Assert.That(widget, Is.TypeOf<PartialViewWidget>());
            Assert.That(((PartialViewWidget)widget).ViewName, Is.EqualTo(StoreReportDashboardWidget.ViewPath));
            Assert.That(widget.Key, Is.EqualTo(instance.Id));
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
        var dashboardWidget = new LatestOrdersDashboardWidget(Moq.Mock.Of<IPermissionService>());
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
                Does.Contain("#admin-dashboard__latest-orders { grid-column: 1 / span 12; grid-row: 6 / span 1; min-height: 0px; align-self: stretch; }"));
            Assert.That(css, Does.Not.Contain("#admin-dashboard-grid >"));
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

#nullable disable

/// <summary>
/// Verifies consent report denominators, legacy records and protection of the underlying customer data.
/// </summary>
[TestFixture]
public class DashboardCookieConsentsViewComponentTests
{
    private static string Decision(bool required = true, bool analytics = true, bool ads = true)
        => JsonSerializer.Serialize(new ConsentCookie
        {
            AllowRequired = required,
            AllowAnalytics = analytics,
            AllowThirdParty = false,
            AdUserDataConsent = ads,
            AdPersonalizationConsent = ads,
            ConsentedOn = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc)
        });

    /// <summary>
    /// Malformed and incomplete records do not inflate the denominator or invent refusals.
    /// </summary>
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("{broken}")]
    [TestCase("null")]
    [TestCase("[]")]
    [TestCase("{}")]
    [TestCase("{\"AllowRequired\":true,\"AllowAnalytics\":true,\"AllowThirdParty\":false}")]
    [TestCase("{\"ConsentedOn\":\"2026-10-01T12:00:00Z\"}")]
    [TestCase("{\"AllowRequired\":true,\"AllowAnalytics\":\"true\",\"AllowThirdParty\":false,\"ConsentedOn\":\"2026-10-01T12:00:00Z\"}")]
    [TestCase("{\"AllowRequired\":true,\"AllowAnalytics\":true,\"AllowThirdParty\":false,\"ConsentedOn\":\"invalid\"}")]
    [TestCase("{\"AllowRequired\":true,\"AllowAnalytics\":true,\"AllowThirdParty\":false,\"ConsentedOn\":\"0001-01-01T00:00:00\"}")]
    public async Task Invalid_Records_Do_Not_Affect_Percentages(string json)
    {
        var report = await RenderReportAsync(Decision(), json);
        Assert.Multiple(() =>
        {
            Assert.That(report.TotalConsents, Is.EqualTo(1));
            Assert.That(report.SkippedConsents, Is.EqualTo(1));
            Assert.That(report.Categories[1].AcceptedCount, Is.EqualTo(1));
            Assert.That(report.Categories[1].DeclinedCount, Is.Zero);
            Assert.That(report.Categories[1].AcceptedPercentage, Is.EqualTo(100m));
        });
    }

    /// <summary>
    /// Missing optional flags in old records remain unknown, including for case-insensitive JSON property names.
    /// </summary>
    [Test]
    public async Task Legacy_Records_Do_Not_Count_Missing_Categories_As_Declined()
    {
        const string legacy = """{"allowRequired":true,"allowAnalytics":false,"allowThirdParty":true,"consentedOn":"2026-10-01T12:00:00Z"}""";
        var report = await RenderReportAsync(legacy);
        Assert.That(report.Categories[3].AcceptedCount + report.Categories[3].DeclinedCount, Is.Zero);
        report = await RenderReportAsync(legacy, Decision());
        Assert.Multiple(() =>
        {
            Assert.That(report.TotalConsents, Is.EqualTo(2));
            Assert.That(report.Categories[1].AcceptedPercentage, Is.EqualTo(50m));
            Assert.That(report.Categories[3].AcceptedPercentage, Is.EqualTo(100m));
            Assert.That(report.Categories[3].DeclinedCount, Is.Zero);
            Assert.That(report.SkippedConsents, Is.Zero);
        });
    }

    /// <summary>
    /// Required-cookie counts reflect stored values and rounded percentages always complement one another.
    /// </summary>
    [Test]
    public async Task Counts_Reflect_Actual_Answers_And_Rounded_Percentages_Add_Up()
    {
        var report = await RenderReportAsync(Decision(), Decision(analytics: false), Decision(required: false, analytics: false));
        Assert.Multiple(() =>
        {
            Assert.That(report.Categories[0].AcceptedCount, Is.EqualTo(2));
            Assert.That(report.Categories[0].DeclinedCount, Is.EqualTo(1));
            Assert.That(report.Categories[1].AcceptedPercentage, Is.EqualTo(33.3m));
            Assert.That(report.Categories[1].DeclinedPercentage, Is.EqualTo(66.7m));
            Assert.That(report.Categories.All(category => category.AcceptedPercentage + category.DeclinedPercentage == 100m), Is.True);
        });
    }

    /// <summary>
    /// An empty report has no invented acceptances or refusals and cannot divide by zero.
    /// </summary>
    [Test]
    public async Task Empty_Report_Has_No_Percentages()
    {
        var report = await RenderReportAsync();
        Assert.That(report.Categories.All(category => category.AcceptedPercentage == 0 && category.DeclinedPercentage == 0), Is.True);
    }

    /// <summary>
    /// Direct component invocation requires the widget permission before reaching the database.
    /// </summary>
    [Test]
    public async Task Direct_Invocation_Requires_Widget_Permission()
    {
        var permissions = new Mock<IPermissionService>(MockBehavior.Strict);
        permissions.Setup(x => x.AuthorizeAsync(Permissions.Dashboard.Widgets.Customers.CookieConsents, null, false)).ReturnsAsync(false);
        var services = new Mock<ICommonServices>(MockBehavior.Strict);
        services.SetupGet(x => x.Permissions).Returns(permissions.Object);
        using var provider = new ServiceCollection().AddLogging().AddSingleton(services.Object).BuildServiceProvider();
        var component = new DashboardCookieConsentsViewComponent(null)
        {
            ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { RequestServices = provider } }
            }
        };
        var result = await component.InvokeAsync();
        Assert.That(((ContentViewComponentResult)result).Content, Is.Empty);
        permissions.Verify(x => x.AuthorizeAsync(Permissions.Dashboard.Widgets.Customers.CookieConsents, null, false), Times.Once);
        services.VerifyGet(x => x.Permissions, Times.Once);
        services.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Cached_Report_Does_Not_Read_Database()
    {
        var report = new DashboardCookieConsentModel { TotalConsents = 3 };
        var cache = new Mock<ICacheManager>(MockBehavior.Strict);
        cache.Setup(x => x.GetAsync("admin:dashboard:cookieconsents",
            It.IsAny<Func<CacheEntryOptions, Task<DashboardCookieConsentModel>>>(), false, false))
            .ReturnsAsync(report);
        var services = new Mock<ICommonServices> { DefaultValue = DefaultValue.Mock };
        services.SetupGet(x => x.Cache).Returns(cache.Object);
        services.Setup(x => x.Permissions.AuthorizeAsync(Permissions.Dashboard.Widgets.Customers.CookieConsents, null, false)).ReturnsAsync(true);
        using var provider = new ServiceCollection().AddLogging().AddSingleton(services.Object).BuildServiceProvider();
        var component = new TestCookieConsentsViewComponent(null)
        {
            ViewEngine = Mock.Of<ICompositeViewEngine>(),
            ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { RequestServices = provider } }
            }
        };

        var result = (ViewViewComponentResult)await component.InvokeAsync();

        Assert.That(result.ViewData.Model, Is.SameAs(report));
        cache.VerifyAll();
    }

    private static async Task<DashboardCookieConsentModel> RenderReportAsync(params string[] decisions)
    {
        await using var db = new ConsentTestContext();
        for (int i = 0; i < decisions.Length; i++)
        {
            db.Customers.Add(new Customer { Id = i + 1 });
            db.GenericAttributes.Add(new GenericAttribute
            {
                EntityId = i + 1,
                KeyGroup = nameof(Customer),
                Key = SystemCustomerAttributeNames.CookieConsent,
                Value = decisions[i],
                StoreId = 0
            });
        }
        await db.SaveChangesAsync();

        var services = new Mock<ICommonServices> { DefaultValue = DefaultValue.Mock };
        services.SetupGet(x => x.Cache).Returns(NullCache.Instance);
        services.Setup(x => x.Permissions.AuthorizeAsync(Permissions.Dashboard.Widgets.Customers.CookieConsents, null, false)).ReturnsAsync(true);
        using var provider = new ServiceCollection().AddLogging().AddSingleton(services.Object).BuildServiceProvider();
        var component = new TestCookieConsentsViewComponent(db)
        {
            ViewEngine = Mock.Of<ICompositeViewEngine>(),
            ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { RequestServices = provider } }
            }
        };
        var result = (ViewViewComponentResult)await component.InvokeAsync();
        return (DashboardCookieConsentModel)result.ViewData.Model;
    }

    private sealed class TestCookieConsentsViewComponent(SmartDbContext db) : DashboardCookieConsentsViewComponent(db)
    {
        protected override bool PublishEvents => false;
    }

    private sealed class ConsentTestContext() : SmartDbContext(new DbContextOptionsBuilder<SmartDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder builder) { }

        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
            => builder.Conventions.Remove<Microsoft.EntityFrameworkCore.Metadata.Conventions.IndexAttributeConvention>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            foreach (var type in builder.Model.GetEntityTypes().Select(entity => entity.ClrType).ToArray())
            {
                builder.Ignore(type);
            }
            var customer = builder.Entity<Customer>();
            customer.HasBaseType((Type)null).HasKey(x => x.Id);
            foreach (var property in typeof(Customer).GetProperties()
                .Where(property => property.Name != nameof(Customer.Id)
                    && property.Name != nameof(Customer.Deleted)
                    && property.Name != nameof(Customer.IsSystemAccount)))
            {
                customer.Ignore(property.Name);
            }
            builder.Entity<GenericAttribute>().HasBaseType((Type)null).HasKey(x => x.Id);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
            => SaveChangesCoreAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}

#nullable disable

[TestFixture]
public class DashboardContactRequestsViewComponentTests
{
    /// <summary>
    /// Rendering filters stores and missing messages before limiting the result, so older contacts can move up.
    /// </summary>
    [TestCase(true)]
    [TestCase(false)]
    public async Task Rendering_Selects_Latest_Existing_Contacts_From_Authorized_Stores(bool restricted)
    {
        await using var db = new ContactRequestsTestContext();
        db.Stores.AddRange(new Store { Id = 1, Name = "First", Url = "https://first.example" },
            new Store { Id = 2, Name = "Second", Url = "https://second.example" });
        foreach (int id in Enumerable.Range(1, 8))
        {
            db.QueuedEmails.Add(new QueuedEmail
            {
                Id = id, From = "shop@example.com", To = "shop@example.com", ReplyTo = id % 2 == 0 ? "system@example.com" : null,
                CreatedOnUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(id)
            });
            db.GenericAttributes.Add(Marker(id, id == 8 ? 2 : 1));
        }
        db.GenericAttributes.Add(Marker(999, 1));
        await db.SaveChangesAsync();
        db.QueuedEmails.Remove(await db.QueuedEmails.FindAsync(7));
        await db.SaveChangesAsync();

        var services = new Mock<ICommonServices> { DefaultValue = DefaultValue.Mock };
        services.Setup(x => x.Permissions.AuthorizeAsync(Permissions.Dashboard.Widgets.Customers.ContactRequests, null, false)).ReturnsAsync(true);
        services.Setup(x => x.WorkContext.CurrentCustomer).Returns(new Customer { Id = 42 });
        services.Setup(x => x.StoreMappingService.GetAuthorizedStoreIdsAsync(nameof(Customer), 42))
            .ReturnsAsync(restricted ? new[] { 1 } : Array.Empty<int>());
        services.Setup(x => x.DateTimeHelper.ConvertToUserTime(It.IsAny<DateTime>(), DateTimeKind.Utc))
            .Returns((DateTime value, DateTimeKind kind) => value);
        using var provider = new ServiceCollection().AddSingleton(services.Object).BuildServiceProvider();
        var component = new TestContactRequestsViewComponent(db)
        {
            ViewEngine = Mock.Of<ICompositeViewEngine>(),
            ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { RequestServices = provider } }
            }
        };
        var result = (ViewViewComponentResult)await component.InvokeAsync();
        var model = (IReadOnlyList<DashboardContactRequestModel>)result.ViewData.Model;
        Assert.Multiple(() =>
        {
            Assert.That(model.Select(x => x.MessageId), Is.EqualTo(restricted ? new[] { 6, 5, 4, 3, 2 } : new[] { 8, 6, 5, 4, 3 }));
            Assert.That(model.Select(x => x.Email), Is.All.EqualTo("form@example.com"));
        });
    }

    private static GenericAttribute Marker(int messageId, int storeId)
        => new()
        {
            EntityId = messageId, KeyGroup = nameof(QueuedEmail), Key = ContactRequestsDashboardHook.AttributeKey,
            StoreId = storeId, Value = "form@example.com"
        };

    /// <summary>
    /// The widget permission is checked before looking up stores, IDs or emails.
    /// </summary>
    [Test]
    public async Task Direct_Invocation_Requires_Widget_Permission()
    {
        var permissions = new Mock<IPermissionService>(MockBehavior.Strict);
        permissions.Setup(x => x.AuthorizeAsync(Permissions.Dashboard.Widgets.Customers.ContactRequests, null, false)).ReturnsAsync(false);
        var services = new Mock<ICommonServices>(MockBehavior.Strict);
        services.SetupGet(x => x.Permissions).Returns(permissions.Object);
        using var provider = new ServiceCollection().AddSingleton(services.Object).BuildServiceProvider();
        var component = new DashboardContactRequestsViewComponent(null)
        {
            ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { RequestServices = provider } }
            }
        };
        var result = await component.InvokeAsync();
        Assert.That(((ContentViewComponentResult)result).Content, Is.Empty);
        permissions.Verify(x => x.AuthorizeAsync(Permissions.Dashboard.Widgets.Customers.ContactRequests, null, false), Times.Once);
        services.VerifyGet(x => x.Permissions, Times.Once);
        services.VerifyNoOtherCalls();
    }

    private sealed class TestContactRequestsViewComponent(SmartDbContext db) : DashboardContactRequestsViewComponent(db)
    {
        protected override bool PublishEvents => false;
    }

}

#nullable disable

[TestFixture]
public class ContactRequestsDashboardHookTests
{
    private static MessageQueuingEvent Contact(int storeId = 1, bool testMode = false)
        => new()
        {
            QueuedEmail = new QueuedEmail(),
            MessageModel = new TemplateModel
            {
                ["Message"] = new NamedModelPart("Message") { ["SenderEmail"] = "form@example.com" }
            },
            MessageContext = new MessageContext
            {
                MessageTemplateName = MessageTemplateNames.SystemContactUs,
                StoreId = storeId,
                TestMode = testMode
            }
        };

    private static IHookedEntity Entry(QueuedEmail email, Smartstore.Data.EntityState state = Smartstore.Data.EntityState.Added)
    {
        var entry = new Mock<IHookedEntity>();
        entry.SetupGet(x => x.Entity).Returns(email);
        entry.SetupGet(x => x.EntityType).Returns(typeof(QueuedEmail));
        entry.SetupGet(x => x.InitialState).Returns(state);
        return entry.Object;
    }

    /// <summary>
    /// Only genuine contact messages with a resolved store reach the post-save tracker.
    /// </summary>
    [TestCase("System.ContactUs", false, 1, true)]
    [TestCase("system.contactus", false, 2, true)]
    [TestCase("System.ContactUs", true, 1, false)]
    [TestCase("OrderPlaced.StoreOwnerNotification", false, 1, false)]
    [TestCase("System.ContactUs", false, 0, false)]
    public void Event_Recognizes_The_Template_Without_Writing(string template, bool testMode, int storeId, bool expected)
    {
        var attributes = new Mock<IGenericAttributeService>(MockBehavior.Strict);
        var hook = new ContactRequestsDashboardHook(attributes.Object, null);
        var message = Contact(storeId, testMode);
        message.MessageContext.MessageTemplateName = template;
        hook.HandleEvent(message);
        Assert.That(message.QueuedEmail.GetHookState(ContactRequestsDashboardHook.AttributeKey),
            expected ? Is.EqualTo((storeId, "form@example.com")) : Is.Null);
        attributes.VerifyNoOtherCalls();
    }

    /// <summary>
    /// The resolved template and store take precedence over their initially requested identifiers.
    /// </summary>
    [Test]
    public void Event_Uses_The_Actual_Template_And_Store()
    {
        var hook = new ContactRequestsDashboardHook(Mock.Of<IGenericAttributeService>(), null);
        var message = Contact();
        message.MessageContext.MessageTemplateName = "Other.Template";
        message.MessageContext.MessageTemplate = new MessageTemplate { Name = MessageTemplateNames.SystemContactUs };
        message.MessageContext.Store = new Store { Id = 7 };
        hook.HandleEvent(message);
        Assert.That(message.QueuedEmail.GetHookState(ContactRequestsDashboardHook.AttributeKey), Is.EqualTo((7, "form@example.com")));
    }

    /// <summary>
    /// Missing form addresses must not be replaced by the system sender or template reply address.
    /// </summary>
    [TestCase(null)]
    [TestCase("")]
    public async Task Missing_Form_Address_Does_Not_Create_A_Marker(string email)
    {
        var attributes = new Mock<IGenericAttributeService>(MockBehavior.Strict);
        var hook = new ContactRequestsDashboardHook(attributes.Object, null);
        var message = Contact();
        message.MessageModel["Message"] = new NamedModelPart("Message") { ["SenderEmail"] = email };
        message.QueuedEmail.Id = 1;
        message.QueuedEmail.From = "system@example.com";
        message.QueuedEmail.ReplyTo = "reply@example.com";

        hook.HandleEvent(message);

        Assert.That(message.QueuedEmail.GetHookState(ContactRequestsDashboardHook.AttributeKey), Is.Null);
        Assert.That(await hook.OnAfterSaveAsync(Entry(message.QueuedEmail), default), Is.EqualTo(HookResult.Ok));
        attributes.VerifyNoOtherCalls();
    }

    /// <summary>
    /// An event without an inserted message cannot store the transient zero identifier.
    /// </summary>
    [Test]
    public async Task Unpersisted_Messages_Are_Not_Remembered()
    {
        var attributes = new Mock<IGenericAttributeService>(MockBehavior.Strict);
        var hook = new ContactRequestsDashboardHook(attributes.Object, null);
        var message = Contact();
        hook.HandleEvent(message);
        Assert.That(await hook.OnAfterSaveAsync(Entry(message.QueuedEmail), default), Is.EqualTo(HookResult.Ok));
        attributes.VerifyNoOtherCalls();
    }

    /// <summary>
    /// An unrelated email must not disable contact tracking for subsequent saves in the shared hook registry.
    /// </summary>
    [Test]
    public async Task Unrelated_Email_Does_Not_Disable_Contact_Hook()
    {
        await using var db = new ContactRequestsTestContext();
        var storeContext = new Mock<IStoreContext>();
        storeContext.SetupGet(x => x.CurrentStore).Returns(new Store { Id = 1 });
        var attributes = new GenericAttributeService(db, storeContext.Object, NullEventPublisher.Instance);
        var hook = new ContactRequestsDashboardHook(attributes, db);
        var metadata = HookMetadata.Create<ContactRequestsDashboardHook, SmartDbContext>(typeof(QueuedEmail), HookImportance.Important);
        var registry = new DefaultDbHookRegistry([new Lazy<IDbSaveHook, HookMetadata>(() => hook, metadata)]);
        var activator = new Mock<IDbHookActivator>();
        activator.Setup(x => x.Activate(metadata)).Returns(hook);
        var processor = new DefaultDbHookProcessor(registry, activator.Object);

        await processor.SavedChangesAsync([Entry(new QueuedEmail { Id = 1 })]);

        var message = Contact(2);
        hook.HandleEvent(message);
        message.QueuedEmail.Id = 2;
        await processor.SavedChangesAsync([Entry(message.QueuedEmail)]);

        var marker = await db.GenericAttributes.AsNoTracking().SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(marker.EntityId, Is.EqualTo(2));
            Assert.That(marker.KeyGroup, Is.EqualTo(nameof(QueuedEmail)));
            Assert.That(marker.Key, Is.EqualTo(ContactRequestsDashboardHook.AttributeKey));
            Assert.That(marker.StoreId, Is.EqualTo(2));
            Assert.That(marker.Value, Is.EqualTo("form@example.com"));
        });
    }

    /// <summary>
    /// Every message receives its own store-specific marker, including messages beyond the display limit.
    /// </summary>
    [Test]
    public async Task Post_Save_Persists_One_Marker_Per_Message()
    {
        await using var db = new ContactRequestsTestContext();
        var storeContext = new Mock<IStoreContext>();
        storeContext.SetupGet(x => x.CurrentStore).Returns(new Store { Id = 1 });
        var attributes = new GenericAttributeService(db, storeContext.Object, NullEventPublisher.Instance);
        var hook = new ContactRequestsDashboardHook(attributes, db);
        attributes.GetAttributesForEntity(nameof(QueuedEmail), 1).Set("Unrelated", "preserve-me");

        foreach (int id in Enumerable.Range(1, 8).Concat([8]))
        {
            var message = Contact();
            hook.HandleEvent(message);
            message.QueuedEmail.Id = id;
            await hook.OnAfterSaveAsync(Entry(message.QueuedEmail), default);
        }
        var otherStore = Contact(2);
        hook.HandleEvent(otherStore);
        otherStore.QueuedEmail.Id = 101;
        await hook.OnAfterSaveAsync(Entry(otherStore.QueuedEmail), default);

        // A subject alone must never identify a contact message, including manually requeued copies.
        var unrelated = new QueuedEmail { Id = 999, Subject = "Kontaktanfrage / Contact us" };
        Assert.That(await hook.OnAfterSaveAsync(Entry(unrelated), default), Is.EqualTo(HookResult.Ok));
        db.ChangeTracker.Clear();
        var saved = await db.GenericAttributes.AsNoTracking().ToListAsync();
        var markers = saved.Where(x => x.Key == ContactRequestsDashboardHook.AttributeKey).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(markers, Has.Length.EqualTo(9));
            Assert.That(markers.Where(x => x.StoreId == 1).Select(x => x.EntityId), Is.EquivalentTo(Enumerable.Range(1, 8)));
            Assert.That(markers.Single(x => x.StoreId == 2).EntityId, Is.EqualTo(101));
            Assert.That(markers.All(x => x.KeyGroup == nameof(QueuedEmail) && x.Value == "form@example.com"), Is.True);
            Assert.That(saved.Single(x => x.Key == "Unrelated").Value, Is.EqualTo("preserve-me"));
        });
    }

    /// <summary>
    /// Batch deletion removes only contact markers for deleted messages, preserving unrelated attributes.
    /// </summary>
    [TestCase(2)]
    [TestCase(260)]
    public async Task Deletion_Removes_Only_Matching_Contact_Markers(int count)
    {
        await using var db = new ContactRequestsTestContext();
        db.GenericAttributes.AddRange(Enumerable.Range(1, count).Select(id => Marker(id, id % 2 + 1)));
        db.GenericAttributes.AddRange(
            Marker(count + 1, 1),
            new GenericAttribute { EntityId = 1, KeyGroup = nameof(Store), Key = ContactRequestsDashboardHook.AttributeKey, Value = "True" },
            new GenericAttribute { EntityId = 1, KeyGroup = nameof(QueuedEmail), Key = "Unrelated", Value = "preserve-me" });
        await db.SaveChangesAsync();
        var hook = new ContactRequestsDashboardHook(Mock.Of<IGenericAttributeService>(), db);
        var deleted = Enumerable.Range(1, count)
            .Select(id => Entry(new QueuedEmail { Id = id }, Smartstore.Data.EntityState.Deleted))
            .ToArray();
        foreach (var entry in deleted)
        {
            Assert.That(await hook.OnAfterSaveAsync(entry, default), Is.EqualTo(HookResult.Ok));
        }
        await hook.OnAfterSaveCompletedAsync(deleted.Append(Entry(new QueuedEmail { Id = count + 1 })), default);
        var saved = await db.GenericAttributes.AsNoTracking().ToListAsync();
        Assert.Multiple(() =>
        {
            Assert.That(saved, Has.Count.EqualTo(3));
            Assert.That(saved.Any(x => x.EntityId == count + 1 && x.Key == ContactRequestsDashboardHook.AttributeKey), Is.True);
            Assert.That(saved.Any(x => x.KeyGroup == nameof(Store)), Is.True);
            Assert.That(saved.Any(x => x.Key == "Unrelated"), Is.True);
        });
    }

    private static GenericAttribute Marker(int messageId, int storeId)
        => new()
        {
            EntityId = messageId, KeyGroup = nameof(QueuedEmail), Key = ContactRequestsDashboardHook.AttributeKey,
            StoreId = storeId, Value = "form@example.com"
        };

}

#nullable disable

internal sealed class ContactRequestsTestContext : SmartDbContext
{
    public ContactRequestsTestContext()
        : base(new DbContextOptionsBuilder<SmartDbContext>().UseSqlite("Data Source=:memory:").Options)
    {
        Database.OpenConnection();
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder builder) { }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        foreach (var type in builder.Model.GetEntityTypes().Select(entity => entity.ClrType)
            .Where(type => type != typeof(GenericAttribute) && type != typeof(QueuedEmail) && type != typeof(Store)).ToArray())
        {
            builder.Ignore(type);
        }
        builder.Entity<GenericAttribute>().HasBaseType((Type)null).HasKey(x => x.Id);
        builder.Entity<QueuedEmail>().HasBaseType((Type)null).HasKey(x => x.Id);
        builder.Entity<QueuedEmail>().Ignore(x => x.EmailAccount).Ignore(x => x.Attachments);
        builder.Entity<Store>().HasBaseType((Type)null).HasKey(x => x.Id);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        => SaveChangesCoreAsync(acceptAllChangesOnSuccess, cancellationToken);
}
