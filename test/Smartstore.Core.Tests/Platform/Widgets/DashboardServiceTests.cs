#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Widgets;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine;
using Smartstore.IO;
using Smartstore.Json;

namespace Smartstore.Core.Tests.Platform.Widgets;

/// <summary>
/// Verifies dashboard widget registration, layout resolution and render-model preparation.
/// </summary>
[TestFixture]
public sealed class DashboardServiceTests
{
    /// <summary>
    /// Verifies that the registered provider supplies the system-default layout.
    /// </summary>
    [Test]
    public async Task Resolves_Provider_Default_Layout()
    {
        var globalLayout = CreateLayout(DashboardLayoutScope.Global);
        var service = CreateService([], [new TestLayoutProvider(globalLayout)]);

        var result = await service.GetEffectiveLayoutAsync("admin-dashboard", 42);

        Assert.That(result, Is.SameAs(globalLayout));
    }

    /// <summary>
    /// Verifies that a valid global JSON layout overrides the provider default.
    /// </summary>
    [Test]
    public async Task Resolves_Global_Json_Before_Provider_Default()
    {
        var defaultLayout = CreateLayout(DashboardLayoutScope.Global);
        var globalLayout = CreateLayout(DashboardLayoutScope.Global, revision: 7);
        var json = JsonSerializer.Serialize(globalLayout, SmartJsonOptions.CamelCased);
        var service = CreateService([], [new TestLayoutProvider(defaultLayout)], json);

        var result = await service.GetEffectiveLayoutAsync("admin-dashboard", 42);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.SameAs(defaultLayout));
            Assert.That(result.Revision, Is.EqualTo(7));
            Assert.That(result.Scope, Is.EqualTo(DashboardLayoutScope.Global));
        });
    }

    /// <summary>
    /// Verifies that malformed global JSON falls back to the provider default.
    /// </summary>
    [Test]
    public async Task Falls_Back_To_Provider_Default_For_Invalid_Global_Json()
    {
        var defaultLayout = CreateLayout(DashboardLayoutScope.Global);
        var service = CreateService([], [new TestLayoutProvider(defaultLayout)], "{ invalid json }");

        var result = await service.GetEffectiveLayoutAsync("admin-dashboard", 42);

        Assert.That(result, Is.SameAs(defaultLayout));
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
        var service = CreateService([widget], [new TestLayoutProvider(layout)]);

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

        var service = CreateService([widget], [new TestLayoutProvider(layout)]);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.GetDashboardAsync("admin-dashboard", 0));
    }

    /// <summary>
    /// Verifies that identifiers are normalized for safe use in CSS selectors.
    /// </summary>
    [Test]
    public void Sanitizes_Unsafe_Dashboard_And_Instance_Identifiers()
    {
        var layout = new DashboardLayout("admin.dashboard");
        var instance = new DashboardWidgetInstance("42-widget", TestDashboardWidget.SystemName);

        Assert.Multiple(() =>
        {
            Assert.That(layout.Id, Is.EqualTo("admin_dashboard"));
            Assert.That(instance.Id, Is.EqualTo("z2-widget"));
        });
    }

    /// <summary>
    /// Creates a valid dashboard layout for a test scenario.
    /// </summary>
    /// <param name="scope">The scope assigned to the layout.</param>
    /// <param name="customerId">The customer assigned to a user-scoped layout.</param>
    /// <param name="revision">The layout revision.</param>
    /// <param name="widgets">The widget instances contained in the layout.</param>
    /// <returns>The test dashboard layout.</returns>
    private static DashboardLayout CreateLayout(
        DashboardLayoutScope scope,
        int customerId = 0,
        int revision = 0,
        IReadOnlyList<DashboardWidgetInstance>? widgets = null)
    {
        return new DashboardLayout("admin-dashboard")
        {
            Scope = scope,
            CustomerId = customerId,
            Revision = revision,
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

    /// <summary>
    /// Creates a dashboard service with empty user and global layout layers.
    /// </summary>
    /// <param name="widgets">The dashboard widgets registered for the test.</param>
    /// <param name="providers">The dashboard layout providers registered for the test.</param>
    /// <param name="globalLayoutJson">The optional contents of the global layout file.</param>
    /// <returns>The configured dashboard service.</returns>
    private static DashboardService CreateService(
        IEnumerable<IDashboardWidget> widgets,
        IEnumerable<IDashboardLayoutProvider> providers,
        string? globalLayoutJson = null)
    {
        var genericAttributeService = new Mock<IGenericAttributeService>();
        genericAttributeService
            .Setup(x => x.GetAttributesForEntity(It.IsAny<string>(), It.IsAny<int>()))
            .Returns<string, int>((entityName, _) => new GenericAttributeCollection(entityName));

        var fileSystem = new Mock<IFileSystem>();
        fileSystem
            .Setup(x => x.Watch(It.IsAny<string>()))
            .Returns(NullChangeToken.Singleton);
        fileSystem
            .Setup(x => x.FileExists(It.IsAny<string>()))
            .Returns(globalLayoutJson != null);

        if (globalLayoutJson != null)
        {
            var file = new Mock<IFile>();
            file
                .SetupGet(x => x.Exists)
                .Returns(true);
            file
                .Setup(x => x.OpenReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new MemoryStream(Encoding.UTF8.GetBytes(globalLayoutJson)));
            fileSystem
                .Setup(x => x.GetFileAsync(It.IsAny<string>()))
                .ReturnsAsync(file.Object);
        }

        var applicationContext = new Mock<IApplicationContext>();
        applicationContext
            .SetupGet(x => x.AppDataRoot)
            .Returns(fileSystem.Object);

        return new DashboardService(
            widgets,
            providers,
            genericAttributeService.Object,
            applicationContext.Object,
            new MemoryCache(new MemoryCacheOptions()));
    }
}
