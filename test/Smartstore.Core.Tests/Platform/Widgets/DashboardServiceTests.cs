using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Core.Widgets;
using Smartstore.Engine;
using Smartstore.Events;
using Smartstore.IO;
using Smartstore.Json;

namespace Smartstore.Core.Tests.Platform.Widgets;

#nullable enable

/// <summary>
/// Verifies dashboard widget registration, layout resolution and render-model preparation.
/// </summary>
[TestFixture]
public sealed class DashboardServiceTests
{
    /// <summary>
    /// Rendering resolves the effective layout without building the provider default twice.
    /// </summary>
    [Test]
    public async Task Rendering_Resolves_The_Layout_Only_Once()
    {
        var publisher = new Mock<IEventPublisher>();
        publisher.Setup(x => x.PublishAsync(It.IsAny<DashboardLayoutBuiltEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = CreateService([], [new TestLayoutProvider(CreateLayout(DashboardLayoutScope.Global))], eventPublisher: publisher.Object);

        var result = await service.GetDashboardAsync("admin-dashboard", CreateTestCustomer());

        Assert.That(result.Layout.Id, Is.EqualTo("admin-dashboard"));
        publisher.Verify(x => x.PublishAsync(It.IsAny<DashboardLayoutBuiltEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Provider layouts are independent of the admin editor's numeric input limits.
    /// </summary>
    [Test]
    public async Task Provider_Layouts_Accept_Positions_Beyond_Editor_Limits()
    {
        var instance = CreateInstance() with
        {
            Positions =
            [
                new DashboardWidgetPosition { Row = 10001, Size = new DashboardWidgetSize(4, 1001) },
                new DashboardWidgetPosition { MinViewportWidth = 10001, Size = new DashboardWidgetSize(4) }
            ]
        };
        var layout = CreateLayout(DashboardLayoutScope.Global, widgets: [instance]);
        var service = CreateService([], [new TestLayoutProvider(layout)]);

        var result = await service.GetEffectiveLayoutAsync(layout.Id, CreateTestCustomer());

        Assert.That(result.Widgets.Single().Positions, Is.EqualTo(instance.Positions));
    }

    /// <summary>
    /// Unknown dashboards retain the original registration error before accessing customer attributes.
    /// </summary>
    [TestCase("effective")]
    [TestCase("render")]
    [TestCase("state")]
    public void Unknown_Dashboards_Retain_The_Registration_Error(string operation)
    {
        var service = CreateService(Array.Empty<IDashboardWidget>(), Array.Empty<IDashboardLayoutProvider>());
        var customer = new Customer { Id = 42 };
        var error = Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            if (operation == "effective")
                await service.GetEffectiveLayoutAsync("missing", customer);
            else if (operation == "render")
                await service.GetDashboardAsync("missing", customer);
            else
                await service.GetLayoutStateAsync("missing", customer, DashboardLayoutLayer.User);
        });

        Assert.That(error!.Message, Is.EqualTo("Dashboard 'missing' is not registered."));
    }

    /// <summary>
    /// Invalid layer values are argument errors independent of dashboard registration errors.
    /// </summary>
    [Test]
    public void Rejects_An_Undefined_Layout_Layer()
    {
        var service = CreateService([], [new TestLayoutProvider(CreateLayout(DashboardLayoutScope.Global))]);
        var error = Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.GetLayoutStateAsync("admin-dashboard", null, (DashboardLayoutLayer)99));

        Assert.That(error!.ParamName, Is.EqualTo("layer"));
    }

    /// <summary>
    /// A derived renderer can customize layout preparation while retaining normal resolution and edit context.
    /// </summary>
    [Test]
    public async Task Rendering_Dispatches_To_Overridden_Layout_Preparation()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var files = new Mock<IFileSystem>();
        files.Setup(x => x.Watch(It.IsAny<string>())).Returns(NullChangeToken.Singleton);
        var app = new Mock<IApplicationContext>();
        app.SetupGet(x => x.TenantRoot).Returns(files.Object);
        var layout = CreateLayout(DashboardLayoutScope.Global);
        var provider = new Lazy<IDashboardLayoutProvider, DashboardMetadata>(() => new TestLayoutProvider(layout),
            new DashboardMetadata { SystemName = layout.Id });
        var service = new Mock<DashboardService>(app.Object, cache, NullEventPublisher.Instance,
            Array.Empty<Lazy<IDashboardWidget, DashboardMetadata>>(), new[] { provider }) { CallBase = true };
        var custom = new DashboardRenderModel
        {
            Layout = layout,
            Context = new DashboardWidgetContext { DashboardId = layout.Id, IsEditMode = true },
            Widgets = []
        };
        service.Setup(x => x.PrepareDashboardAsync(It.IsAny<DashboardLayout>(), null, true, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(custom));

        Assert.That(await service.Object.GetDashboardAsync(layout.Id, null, true), Is.SameAs(custom));
        service.Verify(x => x.PrepareDashboardAsync(It.IsAny<DashboardLayout>(), null, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that constructing the service does not enumerate dashboard component registrations.
    /// </summary>
    [Test]
    public void Does_Not_Enumerate_Component_Registrations_In_Constructor()
    {
        var widgets = new Mock<IEnumerable<Lazy<IDashboardWidget, DashboardMetadata>>>(MockBehavior.Strict);
        var providers = new Mock<IEnumerable<Lazy<IDashboardLayoutProvider, DashboardMetadata>>>(MockBehavior.Strict);

        var service = CreateService(widgets.Object, providers.Object);

        Assert.That(service, Is.Not.Null);
        widgets.VerifyNoOtherCalls();
        providers.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Verifies that the registered provider supplies the system-default layout.
    /// </summary>
    [Test]
    public async Task Resolves_Provider_Default_Layout()
    {
        var globalLayout = CreateLayout(DashboardLayoutScope.Global);
        var service = CreateService([], [new TestLayoutProvider(globalLayout)]);

        var result = await service.GetEffectiveLayoutAsync("admin-dashboard", CreateTestCustomer());

        Assert.That(result, Is.SameAs(globalLayout));
    }

    /// <summary>
    /// Verifies that consumers can modify a system-default layout before it is validated.
    /// </summary>
    [Test]
    public async Task Publishes_Built_Event_For_Provider_Default()
    {
        var defaultLayout = CreateLayout(DashboardLayoutScope.Global);
        var eventPublisher = new Mock<IEventPublisher>();
        eventPublisher
            .Setup(x => x.PublishAsync(
                It.IsAny<DashboardLayoutBuiltEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback<DashboardLayoutBuiltEvent, CancellationToken>((message, _) =>
            {
                message.Layout.ColumnGap = "2rem";
                message.Layout.Widgets.Add(CreateInstance());
            })
            .Returns(Task.CompletedTask);

        var service = CreateService(
            [],
            [new TestLayoutProvider(defaultLayout)],
            eventPublisher: eventPublisher.Object);

        var result = await service.GetEffectiveLayoutAsync("admin-dashboard", CreateTestCustomer());

        Assert.Multiple(() =>
        {
            Assert.That(result.ColumnGap, Is.EqualTo("2rem"));
            Assert.That(result.Widgets, Has.Count.EqualTo(1));
        });

        eventPublisher.Verify(x => x.PublishAsync(
            It.IsAny<DashboardLayoutBuiltEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
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

        var result = await service.GetEffectiveLayoutAsync("admin-dashboard", CreateTestCustomer());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.SameAs(defaultLayout));
            Assert.That(result.Revision, Is.EqualTo(7));
            Assert.That(result.Scope, Is.EqualTo(DashboardLayoutScope.Global));
        });
    }

    /// <summary>
    /// Verifies that a valid global layout does not activate the system-default layout provider.
    /// </summary>
    [Test]
    public async Task Resolves_Global_Json_Without_Activating_Provider()
    {
        var providerActivated = false;
        var globalLayout = CreateLayout(DashboardLayoutScope.Global, revision: 7);
        var json = JsonSerializer.Serialize(globalLayout, SmartJsonOptions.CamelCased);
        var service = CreateService(
            Array.Empty<Lazy<IDashboardWidget, DashboardMetadata>>(),
            [
                new Lazy<IDashboardLayoutProvider, DashboardMetadata>(
                    () =>
                    {
                        providerActivated = true;
                        return new TestLayoutProvider(CreateLayout(DashboardLayoutScope.Global));
                    },
                    new DashboardMetadata { SystemName = "admin-dashboard" })
            ],
            json);

        var result = await service.GetEffectiveLayoutAsync("admin-dashboard", CreateTestCustomer());

        Assert.Multiple(() =>
        {
            Assert.That(result.Revision, Is.EqualTo(7));
            Assert.That(providerActivated, Is.False);
        });
    }

    /// <summary>
    /// Verifies that the complete persisted layout graph survives a JSON roundtrip.
    /// </summary>
    [Test]
    public void Roundtrips_Complete_Layout()
    {
        var instance = new DashboardWidgetInstance("orders", TestDashboardWidget.SystemName)
        {
            Order = 3,
            SettingsVersion = 2,
            Settings = new JsonObject
            {
                ["limit"] = 5,
                ["filters"] = new JsonObject { ["paid"] = true }
            },
            Policy = new DashboardWidgetPolicy
            {
                IsRequired = true,
                AllowMove = false,
                AllowConfigure = true
            },
            Positions =
            [
                new DashboardWidgetPosition
                {
                    Size = new DashboardWidgetSize(12, 1)
                },
                new DashboardWidgetPosition
                {
                    MinViewportWidth = 992,
                    Column = 2,
                    Row = 1,
                    Size = new DashboardWidgetSize(6, 2)
                }
            ]
        };
        var layout = CreateLayout(DashboardLayoutScope.User, customerId: 42, revision: 7, widgets: [instance]);
        layout.ColumnCount = 16;
        layout.GridTemplateColumns = "repeat(16, minmax(0, 1fr))";
        layout.ColumnGap = "2rem";
        layout.RowGap = "3rem";
        layout.GridAutoRows = "minmax(10rem, auto)";
        var service = CreateService(Array.Empty<IDashboardWidget>(), Array.Empty<IDashboardLayoutProvider>());

        var json = service.Serialize(layout);
        var result = service.Deserialize(json)!;
        var resultInstance = result.Widgets.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(layout.Id));
            Assert.That(result.Scope, Is.EqualTo(DashboardLayoutScope.User));
            Assert.That(result.Version, Is.EqualTo(layout.Version));
            Assert.That(result.Revision, Is.EqualTo(7));
            Assert.That(result.CustomerId, Is.EqualTo(42));
            Assert.That(result.ColumnCount, Is.EqualTo(16));
            Assert.That(result.GridTemplateColumns, Is.EqualTo(layout.GridTemplateColumns));
            Assert.That(result.ColumnGap, Is.EqualTo("2rem"));
            Assert.That(result.RowGap, Is.EqualTo("3rem"));
            Assert.That(result.GridAutoRows, Is.EqualTo("minmax(10rem, auto)"));
            Assert.That(resultInstance.Id, Is.EqualTo("orders"));
            Assert.That(resultInstance.WidgetSystemName, Is.EqualTo(TestDashboardWidget.SystemName));
            Assert.That(resultInstance.Order, Is.EqualTo(3));
            Assert.That(resultInstance.SettingsVersion, Is.EqualTo(2));
            Assert.That(resultInstance.Settings["limit"]!.GetValue<int>(), Is.EqualTo(5));
            Assert.That(resultInstance.Settings["filters"]!["paid"]!.GetValue<bool>(), Is.True);
            Assert.That(resultInstance.Policy.IsRequired, Is.True);
            Assert.That(resultInstance.Policy.AllowMove, Is.False);
            Assert.That(resultInstance.Policy.AllowConfigure, Is.True);
            Assert.That(resultInstance.Positions, Is.EqualTo(instance.Positions));
        });
    }

    /// <summary>
    /// Verifies that a structurally incomplete global layout falls back to the provider default.
    /// </summary>
    [Test]
    public async Task Falls_Back_For_Null_Widget_Collection()
    {
        const string json = """{"id":"admin-dashboard","scope":"Global","widgets":null}""";
        var defaultLayout = CreateLayout(DashboardLayoutScope.Global);
        var service = CreateService([], [new TestLayoutProvider(defaultLayout)], json);

        var result = await service.GetEffectiveLayoutAsync("admin-dashboard", CreateTestCustomer());

        Assert.That(result, Is.SameAs(defaultLayout));
    }

    /// <summary>
    /// Verifies that malformed global JSON falls back to the provider default.
    /// </summary>
    [Test]
    public async Task Falls_Back_To_Provider_Default_For_Invalid_Global_Json()
    {
        var defaultLayout = CreateLayout(DashboardLayoutScope.Global);
        var service = CreateService([], [new TestLayoutProvider(defaultLayout)], "{ invalid json }");

        var result = await service.GetEffectiveLayoutAsync("admin-dashboard", CreateTestCustomer());

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

        var result = await service.GetDashboardAsync("admin-dashboard", CreateTestCustomer(), true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Context.DashboardId, Is.EqualTo("admin-dashboard"));
            Assert.That(result.Context.Customer?.Id, Is.EqualTo(42));
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
            async () => await service.GetDashboardAsync("admin-dashboard", null));
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
            Widgets = widgets?.ToList() ?? []
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
    /// <param name="eventPublisher">The optional event publisher used by the service.</param>
    /// <returns>The configured dashboard service.</returns>
    private static TestDashboardService CreateService(
        IEnumerable<IDashboardWidget> widgets,
        IEnumerable<IDashboardLayoutProvider> providers,
        string? globalLayoutJson = null,
        IEventPublisher? eventPublisher = null)
    {
        return CreateService(
            widgets.Select(x => new Lazy<IDashboardWidget, DashboardMetadata>(
                () => x,
                new DashboardMetadata { SystemName = x.Descriptor.SystemName })),
            providers.Select(x => new Lazy<IDashboardLayoutProvider, DashboardMetadata>(
                () => x,
                new DashboardMetadata { SystemName = "admin-dashboard" })),
            globalLayoutJson,
            eventPublisher);
    }

    private static TestDashboardService CreateService(
        IEnumerable<Lazy<IDashboardWidget, DashboardMetadata>> widgets,
        IEnumerable<Lazy<IDashboardLayoutProvider, DashboardMetadata>> providers,
        string? globalLayoutJson = null,
        IEventPublisher? eventPublisher = null)
    {
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
            .SetupGet(x => x.TenantRoot)
            .Returns(fileSystem.Object);

        return new TestDashboardService(
            applicationContext.Object,
            new MemoryCache(new MemoryCacheOptions()),
            eventPublisher ?? NullEventPublisher.Instance,
            widgets,
            providers);
    }

    private static Customer CreateTestCustomer(int id = 42)
    {
        var customer = new Customer
        {
            Id = id
        };

        customer.SetGenericAttributes(new GenericAttributeCollection(nameof(Customer)));

        return customer;
    }
}

#nullable disable

/// <summary>
/// Verifies storage isolation, reset previews and last-save-wins behavior using the normal attribute persistence path.
/// </summary>
[TestFixture, NonParallelizable]
public class DashboardLayoutPersistenceTests : ServiceTestBase
{
    private string _directory;
    private LocalFileSystem _files;
    private MemoryCache _cache;
    private TestDashboardService _service;
    private Customer _customer;

    /// <summary>
    /// Creates a unique tenant directory below the workspace scratch directory.
    /// </summary>
    [SetUp]
    public void CreateDashboard()
    {
        var workspace = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (workspace != null && !Directory.Exists(Path.Combine(workspace.FullName, "Smartstore", "src")))
            workspace = workspace.Parent;
        Assert.That(workspace, Is.Not.Null);
        _directory = Path.Combine(workspace.FullName, ".temp", "dashboard-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _files = new LocalFileSystem(_directory);
        _cache = new MemoryCache(new MemoryCacheOptions());
        var app = new Mock<IApplicationContext>();
        app.SetupGet(x => x.TenantRoot).Returns(_files);
        _service = new TestDashboardService(app.Object, _cache, NullEventPublisher.Instance,
            [new Lazy<IDashboardWidget, DashboardMetadata>(() => new TestDashboardWidget(), new DashboardMetadata { SystemName = TestDashboardWidget.SystemName })],
            [new Lazy<IDashboardLayoutProvider, DashboardMetadata>(() => new TestLayoutProvider(DefaultLayout()), new DashboardMetadata { SystemName = "admin-dashboard" })]);
        _customer = new Customer { Id = 91000 + TestContext.CurrentContext.Random.Next(1, 100000) };
        _customer.SetGenericAttributes(new GenericAttributeCollection(
            DbContext.GenericAttributes.Where(x => x.KeyGroup == nameof(Customer) && x.EntityId == _customer.Id), nameof(Customer), _customer.Id, 0));
    }

    /// <summary>
    /// Removes only the uniquely created test directory and releases file watchers.
    /// </summary>
    [TearDown]
    public void ReleaseDashboard()
    {
        _cache?.Dispose();
        _files?.Dispose();
        if (_directory != null && Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    /// <summary>
    /// Creates a small valid provider default with responsive placements.
    /// </summary>
    private static DashboardLayout DefaultLayout()
        => new("admin-dashboard")
        {
            Widgets = [new DashboardWidgetInstance("orders", TestDashboardWidget.SystemName)
            {
                SettingsVersion = 2,
                Positions =
                [
                    new DashboardWidgetPosition { Size = new DashboardWidgetSize(4) },
                    new DashboardWidgetPosition { MinViewportWidth = 992, Column = 4, Size = new DashboardWidgetSize(4) }
                ]
            }]
        };

    /// <summary>
    /// Loads one mutable layer without changing the selected view.
    /// </summary>
    private ValueTask<DashboardLayoutState> State(DashboardLayoutLayer layer, bool reset = false)
        => _service.GetLayoutStateAsync("admin-dashboard", _customer, layer, reset);

    /// <summary>
    /// Writes the current layer.
    /// </summary>
    private Task Save(DashboardLayoutState state, bool reset = false)
        => _service.SaveLayoutAsync(state.Layout, _customer, state.Layer, reset);

    /// <summary>
    /// Personal layouts require no physical file path.
    /// </summary>
    [Test]
    public async Task Personal_Save_Requires_No_Writable_Tenant_Directory()
    {
        var files = new Mock<IFileSystem>(MockBehavior.Strict);
        files.SetupGet(x => x.Root).Returns("read-only-tenant");
        files.Setup(x => x.Watch(It.IsAny<string>())).Returns(Microsoft.Extensions.FileProviders.NullChangeToken.Singleton);
        files.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);
        var app = new Mock<IApplicationContext>();
        app.SetupGet(x => x.TenantRoot).Returns(files.Object);
        _service = new TestDashboardService(app.Object, _cache, NullEventPublisher.Instance,
            [new Lazy<IDashboardWidget, DashboardMetadata>(() => new TestDashboardWidget(), new DashboardMetadata { SystemName = TestDashboardWidget.SystemName })],
            [new Lazy<IDashboardLayoutProvider, DashboardMetadata>(() => new TestLayoutProvider(DefaultLayout()), new DashboardMetadata { SystemName = "admin-dashboard" })]);

        await Save(await State(DashboardLayoutLayer.User));
        Assert.That((await State(DashboardLayoutLayer.User)).Source, Is.EqualTo(DashboardLayoutLayer.User));
        files.Verify(x => x.MapPath(It.IsAny<string>()), Times.Never());
    }

    /// <summary>
    /// Creates a separate customer backed by the test database rather than another customer's attribute collection.
    /// </summary>
    private Customer LoadCustomer(int id)
    {
        var customer = new Customer { Id = id };
        customer.SetGenericAttributes(new GenericAttributeCollection(
            DbContext.GenericAttributes.Where(x => x.KeyGroup == nameof(Customer) && x.EntityId == id), nameof(Customer), id, 0));
        return customer;
    }

    /// <summary>
    /// Saving a dashboard must not discard pending changes to unrelated customer attributes.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task Saving_Personal_Layout_Preserves_Unrelated_Pending_Attributes(bool reset)
    {
        _customer.GenericAttributes.Set("DashboardAcceptance.Unrelated", "original");
        await _customer.GenericAttributes.SaveChangesAsync();
        var state = await State(DashboardLayoutLayer.User);
        _customer.GenericAttributes.Set("DashboardAcceptance.Unrelated", "pending");

        await Save(state, reset);
        Assert.That(_customer.GenericAttributes.Get<string>("DashboardAcceptance.Unrelated"), Is.EqualTo("pending"));
    }

    /// <summary>
    /// An existing layout whose widget settings can be migrated for rendering must remain editable.
    /// </summary>
    [TestCase(DashboardLayoutLayer.User)]
    [TestCase(DashboardLayoutLayer.Global)]
    public async Task Existing_Migratable_Settings_Can_Be_Saved_After_Rendering(DashboardLayoutLayer layer)
    {
        var legacy = DefaultLayout();
        legacy.Scope = layer == DashboardLayoutLayer.User ? DashboardLayoutScope.User : DashboardLayoutScope.Global;
        legacy.CustomerId = layer == DashboardLayoutLayer.User ? _customer.Id : 0;
        legacy.Widgets[0] = legacy.Widgets[0] with { SettingsVersion = 1 };
        string stored = _service.Serialize(legacy);
        if (layer == DashboardLayoutLayer.User)
        {
            _customer.GenericAttributes.Set("DashboardLayout.admin-dashboard", stored);
            await _customer.GenericAttributes.SaveChangesAsync();
        }
        else
        {
            await _files.WriteAllTextAsync("dashboard.admin-dashboard.json", stored);
        }

        var widget = new Mock<IDashboardWidget>();
        widget.SetupGet(x => x.Descriptor).Returns(new TestDashboardWidget().Descriptor);
        widget.Setup(x => x.IsAvailableAsync(It.IsAny<DashboardWidgetContext>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(true));
        widget.Setup(x => x.MigrateSettingsAsync(It.IsAny<System.Text.Json.Nodes.JsonObject>(), 1, It.IsAny<CancellationToken>()))
            .Returns((System.Text.Json.Nodes.JsonObject settings, int version, CancellationToken token) =>
            {
                settings["migrated"] = true;
                return ValueTask.FromResult(settings);
            });
        widget.Setup(x => x.ValidateSettingsAsync(It.IsAny<System.Text.Json.Nodes.JsonObject>(), It.IsAny<CancellationToken>()))
            .Returns((System.Text.Json.Nodes.JsonObject settings, CancellationToken token) =>
                settings["migrated"]?.GetValue<bool>() == true ? ValueTask.CompletedTask
                    : ValueTask.FromException(new InvalidOperationException("The current widget requires migrated settings.")));
        widget.Setup(x => x.CreateWidget(It.IsAny<DashboardWidgetContext>(), It.IsAny<DashboardWidgetInstance>()))
            .Returns(new Smartstore.Core.Widgets.HtmlWidget("test"));
        var app = new Mock<IApplicationContext>();
        app.SetupGet(x => x.TenantRoot).Returns(_files);
        _service = new TestDashboardService(app.Object, _cache, NullEventPublisher.Instance,
            [new Lazy<IDashboardWidget, DashboardMetadata>(() => widget.Object, new DashboardMetadata { SystemName = TestDashboardWidget.SystemName })],
            [new Lazy<IDashboardLayoutProvider, DashboardMetadata>(() => new TestLayoutProvider(DefaultLayout()), new DashboardMetadata { SystemName = "admin-dashboard" })]);

        var rendered = await _service.GetDashboardAsync("admin-dashboard", _customer);
        Assert.That(rendered.Widgets.Single().Instance.SettingsVersion, Is.EqualTo(2));
        var draft = await State(layer);
        await _service.ValidateDraftAsync(draft.Layout, _customer, layer);
        await _service.ValidateDraftAsync(draft.Layout, _customer, layer);
        Assert.That(draft.Layout.Widgets.Single().Settings.ContainsKey("migrated"), Is.False);
        Assert.That(layer == DashboardLayoutLayer.User
            ? _customer.GenericAttributes.Get<string>("DashboardLayout.admin-dashboard")
            : await _files.ReadAllTextAsync("dashboard.admin-dashboard.json"), Is.EqualTo(stored));
        await Save(draft);
        Assert.That((await _service.GetDashboardAsync("admin-dashboard", _customer)).Widgets.Single().Instance.SettingsVersion, Is.EqualTo(2));
    }

    /// <summary>
    /// Shared edits and a personal reset preserve the other customer's selection and personal customization.
    /// </summary>
    [Test]
    public async Task Two_Customers_Keep_Independent_Views_And_Customizations()
    {
        var second = LoadCustomer(_customer.Id + 1000000);
        await Save(await State(DashboardLayoutLayer.Global));
        var personal = await State(DashboardLayoutLayer.User);
        personal.Layout.Widgets.Clear();
        await Save(personal);
        await _service.SelectLayerAsync("admin-dashboard", second, DashboardLayoutLayer.System);

        var shared = await State(DashboardLayoutLayer.Global);
        shared.Layout.Widgets[0] = shared.Layout.Widgets[0] with
        {
            Positions = [new DashboardWidgetPosition { Row = 2, Size = new DashboardWidgetSize(4) }]
        };
        await Save(shared);
        Assert.That((await State(DashboardLayoutLayer.User)).Layout.Widgets, Is.Empty);
        var inherited = await _service.GetLayoutStateAsync("admin-dashboard", second, DashboardLayoutLayer.User);
        Assert.That(inherited.Source, Is.EqualTo(DashboardLayoutLayer.Global));
        Assert.That(inherited.Layout.Widgets.Single().GetPosition(0).Row, Is.EqualTo(2));
        Assert.That(_service.GetSelectedLayer("admin-dashboard", second), Is.EqualTo(DashboardLayoutLayer.System));

        await Save(await State(DashboardLayoutLayer.User, true), true);
        Assert.That((await State(DashboardLayoutLayer.User)).Source, Is.EqualTo(DashboardLayoutLayer.Global));
        Assert.That(_service.GetSelectedLayer("admin-dashboard", second), Is.EqualTo(DashboardLayoutLayer.System));
        Assert.That((await _service.GetEffectiveLayoutAsync("admin-dashboard", second)).Widgets.Single().GetPosition(0).Row, Is.EqualTo(2));
    }

    /// <summary>
    /// Reloaded customers and a new service/cache retain stored layouts and the selected view.
    /// </summary>
    [TestCase(DashboardLayoutLayer.System)]
    [TestCase(DashboardLayoutLayer.Global)]
    [TestCase(DashboardLayoutLayer.User)]
    public async Task Stored_Layouts_And_Selection_Survive_A_Cold_Service(DashboardLayoutLayer selected)
    {
        var shared = await State(DashboardLayoutLayer.Global);
        shared.Layout.Widgets.Clear();
        await Save(shared);
        var personal = await State(DashboardLayoutLayer.User);
        personal.Layout.Widgets.Add(DefaultLayout().Widgets.Single());
        await Save(personal);
        await _service.SelectLayerAsync("admin-dashboard", _customer, selected);
        int customerId = _customer.Id;
        DbContext.ChangeTracker.Clear();
        _customer = LoadCustomer(customerId);
        _cache.Dispose();
        _cache = new MemoryCache(new MemoryCacheOptions());
        var app = new Mock<IApplicationContext>();
        app.SetupGet(x => x.TenantRoot).Returns(_files);
        _service = new TestDashboardService(app.Object, _cache, NullEventPublisher.Instance,
            [new Lazy<IDashboardWidget, DashboardMetadata>(() => new TestDashboardWidget(), new DashboardMetadata { SystemName = TestDashboardWidget.SystemName })],
            [new Lazy<IDashboardLayoutProvider, DashboardMetadata>(() => new TestLayoutProvider(DefaultLayout()), new DashboardMetadata { SystemName = "admin-dashboard" })]);

        Assert.That(_service.GetSelectedLayer("admin-dashboard", _customer), Is.EqualTo(selected));
        Assert.That((await State(DashboardLayoutLayer.Global)).Layout.Widgets, Is.Empty);
        Assert.That((await State(DashboardLayoutLayer.User)).Layout.Widgets, Has.Count.EqualTo(1));
        Assert.That((await _service.GetEffectiveLayoutAsync("admin-dashboard", _customer)).Widgets,
            Has.Count.EqualTo(1));
    }

    /// <summary>
    /// A later save replaces the earlier layout after normal widget validation.
    /// </summary>
    [TestCase(DashboardLayoutLayer.Global)]
    [TestCase(DashboardLayoutLayer.User)]
    public async Task Later_Save_Replaces_An_Earlier_Layout(DashboardLayoutLayer layer)
    {
        var first = await State(layer);
        var second = await State(layer);
        first.Layout.Widgets[0] = first.Layout.Widgets[0] with
        {
            Positions = [new DashboardWidgetPosition { Row = 2, Size = new DashboardWidgetSize(4) }]
        };
        second.Layout.Widgets[0] = second.Layout.Widgets[0] with
        {
            Positions = [new DashboardWidgetPosition { Row = 3, Size = new DashboardWidgetSize(4) }]
        };

        await Save(first);
        await Save(second);
        Assert.That((await State(layer)).Layout.Widgets.Single().GetPosition(0).Row, Is.EqualTo(3));
    }

    /// <summary>
    /// Saving resolves the provider baseline only once for validation and revision assignment.
    /// </summary>
    [TestCase(DashboardLayoutLayer.User)]
    [TestCase(DashboardLayoutLayer.Global)]
    public async Task Saving_Resolves_The_Baseline_Only_Once(DashboardLayoutLayer layer)
    {
        var publisher = new Mock<IEventPublisher>();
        publisher.Setup(x => x.PublishAsync(It.IsAny<DashboardLayoutBuiltEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var provider = new Mock<IDashboardLayoutProvider>();
        provider.Setup(x => x.GetDefaultLayout()).Returns(DefaultLayout);
        var app = new Mock<IApplicationContext>();
        app.SetupGet(x => x.TenantRoot).Returns(_files);
        _service = new TestDashboardService(app.Object, _cache, publisher.Object,
            [new Lazy<IDashboardWidget, DashboardMetadata>(() => new TestDashboardWidget(), new DashboardMetadata { SystemName = TestDashboardWidget.SystemName })],
            [new Lazy<IDashboardLayoutProvider, DashboardMetadata>(() => provider.Object, new DashboardMetadata { SystemName = "admin-dashboard" })]);
        var state = await State(layer);
        publisher.Invocations.Clear();
        provider.Invocations.Clear();

        await Save(state);

        provider.Verify(x => x.GetDefaultLayout(), Times.Once);
        publisher.Verify(x => x.PublishAsync(It.IsAny<DashboardLayoutBuiltEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That((await State(layer)).Layout.Revision, Is.EqualTo(1));
    }

    /// <summary>
    /// Saving and resetting layouts leave the independently selected view unchanged.
    /// </summary>
    [TestCase(DashboardLayoutLayer.User, false)]
    [TestCase(DashboardLayoutLayer.User, true)]
    [TestCase(DashboardLayoutLayer.Global, false)]
    [TestCase(DashboardLayoutLayer.Global, true)]
    public async Task Saving_And_Resetting_Do_Not_Change_View_Selection(DashboardLayoutLayer layer, bool reset)
    {
        await Save(await State(layer));
        await _service.SelectLayerAsync("admin-dashboard", _customer, DashboardLayoutLayer.System);

        await Save(await State(layer, reset), reset);

        Assert.That(_service.GetSelectedLayer("admin-dashboard", _customer), Is.EqualTo(DashboardLayoutLayer.System));
        Assert.That((await State(layer)).Source, Is.EqualTo(reset ? DashboardLayoutLayer.System : layer));
    }

    /// <summary>
    /// Unavailable data widgets are hidden at render time and cannot be removed through a submitted draft.
    /// </summary>
    [Test]
    public async Task Unavailable_Data_Widgets_Are_Hidden_And_Preserved()
    {
        var widget = new Mock<IDashboardWidget>();
        widget.SetupGet(x => x.Descriptor).Returns(new TestDashboardWidget().Descriptor);
        widget.Setup(x => x.IsAvailableAsync(It.IsAny<DashboardWidgetContext>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(false));
        var app = new Mock<IApplicationContext>();
        app.SetupGet(x => x.TenantRoot).Returns(_files);
        _service = new TestDashboardService(app.Object, _cache, NullEventPublisher.Instance,
            [new Lazy<IDashboardWidget, DashboardMetadata>(() => widget.Object, new DashboardMetadata { SystemName = TestDashboardWidget.SystemName })],
            [new Lazy<IDashboardLayoutProvider, DashboardMetadata>(() => new TestLayoutProvider(DefaultLayout()), new DashboardMetadata { SystemName = "admin-dashboard" })]);
        var state = await State(DashboardLayoutLayer.User);
        Assert.That((await _service.PrepareDashboardAsync(state.Layout, _customer)).Widgets, Is.Empty);
        state.Layout.Widgets.Clear();
        Assert.ThrowsAsync<InvalidOperationException>(async () => await Save(state));
        Assert.That((await State(DashboardLayoutLayer.User)).Layout.Widgets, Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Overlapping placements are preserved, while unavailable widget instances remain protected.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task Saving_Preserves_Overlapping_Placements_And_Protects_Unavailable_Widgets(bool hiddenAvailable)
    {
        var layout = new DashboardLayout("admin-dashboard");
        var widgets = new List<Lazy<IDashboardWidget, DashboardMetadata>>();
        foreach (string id in new[] { "visible", "hidden" })
        {
            var widget = new Mock<IDashboardWidget>();
            widget.SetupGet(x => x.Descriptor).Returns(new DashboardWidgetDescriptor(id, id)
            {
                SettingsVersion = id == "hidden" && !hiddenAvailable ? 2 : 1,
                DefaultSize = new DashboardWidgetSize(4),
                MinSize = new DashboardWidgetSize(1),
                MaxSize = new DashboardWidgetSize(12)
            });
            widget.Setup(x => x.IsAvailableAsync(It.IsAny<DashboardWidgetContext>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult(id == "visible" || hiddenAvailable));
            if (id == "hidden" && !hiddenAvailable)
            {
                widget.Setup(x => x.MigrateSettingsAsync(It.IsAny<System.Text.Json.Nodes.JsonObject>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Throws(new InvalidOperationException("Hidden widget settings must not be migrated."));
                widget.Setup(x => x.ValidateSettingsAsync(It.IsAny<System.Text.Json.Nodes.JsonObject>(), It.IsAny<CancellationToken>()))
                    .Throws(new InvalidOperationException("Hidden widget settings must not be validated."));
            }
            widgets.Add(new Lazy<IDashboardWidget, DashboardMetadata>(() => widget.Object, new DashboardMetadata { SystemName = id }));
            layout.Widgets.Add(new DashboardWidgetInstance(id, id)
            {
                Positions = [new DashboardWidgetPosition { Row = id == "visible" ? 20 : 0, Size = new DashboardWidgetSize(4, 20) }]
            });
        }
        var app = new Mock<IApplicationContext>();
        app.SetupGet(x => x.TenantRoot).Returns(_files);
        _service = new TestDashboardService(app.Object, _cache, NullEventPublisher.Instance, widgets,
            [new Lazy<IDashboardLayoutProvider, DashboardMetadata>(() => new TestLayoutProvider(layout), new DashboardMetadata { SystemName = layout.Id })]);
        var state = await State(DashboardLayoutLayer.User);
        var hidden = state.Layout.Widgets[1];
        state.Layout.Widgets[0] = state.Layout.Widgets[0] with { Positions = hidden.Positions };
        await Save(state);
        foreach (var registration in widgets)
        {
            Mock.Get(registration.Value).Verify(x => x.IsAvailableAsync(It.IsAny<DashboardWidgetContext>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        var saved = (await State(DashboardLayoutLayer.User)).Layout;
        Assert.That(saved.Widgets[0].GetPosition(0).Row, Is.Zero);
        Assert.That(saved.Widgets[1].Positions, Is.EqualTo(hidden.Positions));
        Assert.That(saved.Widgets[1].SettingsVersion, Is.EqualTo(hidden.SettingsVersion));
        if (!hiddenAvailable)
        {
            saved.Widgets[1] = hidden with { Positions = [new DashboardWidgetPosition { Row = 1, Size = new DashboardWidgetSize(4, 20) }] };
            Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.SaveLayoutAsync(saved, _customer, DashboardLayoutLayer.User));
        }
    }

    /// <summary>
    /// Verifies independent stored layouts and an independent personal view selection.
    /// </summary>
    [Test]
    public async Task Switching_Views_Preserves_Both_Customizations()
    {
        var shared = await State(DashboardLayoutLayer.Global);
        await Save(shared);
        var personal = await State(DashboardLayoutLayer.User);
        personal.Layout.Widgets.Clear();
        await Save(personal);
        foreach (var layer in new[] { DashboardLayoutLayer.System, DashboardLayoutLayer.Global, DashboardLayoutLayer.User })
        {
            await _service.SelectLayerAsync("admin-dashboard", _customer, layer);
            var selected = await State(_service.GetSelectedLayer("admin-dashboard", _customer));
            Assert.That(selected.Layout.Widgets, Has.Count.EqualTo(layer == DashboardLayoutLayer.User ? 0 : 1));
            Assert.That((await _service.GetEffectiveLayoutAsync("admin-dashboard", _customer)).Widgets, Is.Empty);
            Assert.That((await _service.GetDashboardAsync("admin-dashboard", _customer)).Layout.Widgets, Is.Empty);
        }
        Assert.That(_files.FileExists("dashboard.admin-dashboard.json"), Is.True);
    }

    [TestCase(DashboardLayoutLayer.User)]
    [TestCase(DashboardLayoutLayer.Global)]
    public async Task Reset_Does_Not_Load_The_Provider_Layout(DashboardLayoutLayer layer)
    {
        var provider = new Mock<IDashboardLayoutProvider>(MockBehavior.Strict);
        var app = new Mock<IApplicationContext>();
        app.SetupGet(x => x.TenantRoot).Returns(_files);
        _service = new TestDashboardService(app.Object, _cache, NullEventPublisher.Instance, [],
            [new Lazy<IDashboardLayoutProvider, DashboardMetadata>(() => provider.Object,
                new DashboardMetadata { SystemName = "admin-dashboard" })]);

        await _service.SaveLayoutAsync(new DashboardLayout("admin-dashboard"), _customer, layer, reset: true);

        provider.VerifyNoOtherCalls();
        Assert.That(_files.FileExists("dashboard.admin-dashboard.json"), Is.False);
        Assert.That(_customer.GenericAttributes.Get<string>("DashboardLayout.admin-dashboard"), Is.Null.Or.Empty);
    }

    /// <summary>
    /// Verifies that reset previews are reversible and saving a personal reset leaves the shared layout intact.
    /// </summary>
    [Test]
    public async Task Reset_Is_Only_Persisted_On_Save()
    {
        await Save(await State(DashboardLayoutLayer.Global));
        var personal = await State(DashboardLayoutLayer.User);
        personal.Layout.Widgets.Clear();
        await Save(personal);
        var preview = await State(DashboardLayoutLayer.User, true);
        Assert.That(preview.Source, Is.EqualTo(DashboardLayoutLayer.Global));
        Assert.That(preview.Layout.Widgets, Has.Count.EqualTo(1));
        Assert.That((await State(DashboardLayoutLayer.User)).Layout.Widgets, Is.Empty);
        await Save(preview, true);
        Assert.That((await State(DashboardLayoutLayer.User)).Source, Is.EqualTo(DashboardLayoutLayer.Global));
        Assert.That(_files.FileExists("dashboard.admin-dashboard.json"), Is.True);
    }

    /// <summary>
    /// Verifies that a shared layout can be reset and recreated without deleting the personal layout.
    /// </summary>
    [Test]
    public async Task Global_Reset_And_Recreation_Preserve_The_Personal_Layout()
    {
        await Save(await State(DashboardLayoutLayer.Global));
        await Save(await State(DashboardLayoutLayer.User));
        var preview = await State(DashboardLayoutLayer.Global, true);
        Assert.That(preview.Source, Is.EqualTo(DashboardLayoutLayer.System));
        await Save(preview, true);
        Assert.That((await State(DashboardLayoutLayer.User)).Source, Is.EqualTo(DashboardLayoutLayer.User));
        await Save(await State(DashboardLayoutLayer.Global));
    }

    /// <summary>
    /// Verifies that changing a detached preview cannot mutate the provider default.
    /// </summary>
    [Test]
    public async Task Discarding_A_Draft_Leaves_The_Default_Untouched()
    {
        var draft = await State(DashboardLayoutLayer.User);
        draft.Layout.Widgets.Clear();
        var fresh = await State(DashboardLayoutLayer.User);
        Assert.That(fresh.Source, Is.EqualTo(DashboardLayoutLayer.System));
        Assert.That(fresh.Layout.Widgets, Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Verifies immutable system defaults and server-side widget capability enforcement.
    /// </summary>
    [Test]
    public async Task Rejects_System_Writes_And_Locked_Resizing()
    {
        var system = await State(DashboardLayoutLayer.System);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await Save(system));
        var personal = await State(DashboardLayoutLayer.User);
        var instance = personal.Layout.Widgets.Single();
        personal.Layout.Widgets[0] = instance with
        {
            Positions = [new DashboardWidgetPosition { Size = new DashboardWidgetSize(8) }]
        };
        Assert.ThrowsAsync<InvalidOperationException>(async () => await Save(personal));
        Assert.That((await State(DashboardLayoutLayer.User)).Source, Is.EqualTo(DashboardLayoutLayer.System));
    }

    /// <summary>
    /// Changing row spans or a requested minimum height requires resize permission.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task Rejects_Locked_Height_Changes(bool minimumHeight)
    {
        var state = await State(DashboardLayoutLayer.User);
        var widget = state.Layout.Widgets.Single();
        state.Layout.Widgets[0] = widget with
        {
            Positions = widget.Positions.Select(p => minimumHeight ? p with { MinHeight = 700 } : p with { Size = new DashboardWidgetSize(p.Size.ColumnSpan, 27) }).ToArray()
        };
        Assert.ThrowsAsync<InvalidOperationException>(async () => await Save(state));
    }
    /// <summary>
    /// Disabling automatic stretching is also a protected height change.
    /// </summary>
    [Test]
    public async Task Rejects_Locked_Stretch_Changes()
    {
        var state = await State(DashboardLayoutLayer.User);
        var widget = state.Layout.Widgets.Single();
        state.Layout.Widgets[0] = widget with
        {
            Positions = widget.Positions.Select(p => p with { Stretch = false }).ToArray()
        };
        Assert.ThrowsAsync<InvalidOperationException>(async () => await Save(state));
    }

    /// <summary>
    /// Requested heights survive storage and remain independent between responsive placements and layers.
    /// </summary>
    [TestCase(DashboardLayoutLayer.User)]
    [TestCase(DashboardLayoutLayer.Global)]
    public async Task Requested_Height_Roundtrips_Through_Storage(DashboardLayoutLayer layer)
    {
        var widget = new Mock<IDashboardWidget>();
        widget.SetupGet(x => x.Descriptor).Returns(new DashboardWidgetDescriptor(TestDashboardWidget.SystemName,
            Smartstore.Core.Localization.ResolvableText.Resource("Tests.Height"))
        {
            SettingsVersion = 2,
            DefaultSize = new DashboardWidgetSize(4),
            MinSize = new DashboardWidgetSize(2),
            MaxSize = new DashboardWidgetSize(12)
        });
        widget.Setup(x => x.IsAvailableAsync(It.IsAny<DashboardWidgetContext>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult(true));
        var app = new Mock<IApplicationContext>();
        app.SetupGet(x => x.TenantRoot).Returns(_files);
        _service = new TestDashboardService(app.Object, _cache, NullEventPublisher.Instance,
            [new Lazy<IDashboardWidget, DashboardMetadata>(() => widget.Object, new DashboardMetadata { SystemName = TestDashboardWidget.SystemName })],
            [new Lazy<IDashboardLayoutProvider, DashboardMetadata>(() => new TestLayoutProvider(DefaultLayout()), new DashboardMetadata { SystemName = "admin-dashboard" })]);
        var state = await State(layer);
        var instance = state.Layout.Widgets.Single();
        state.Layout.Widgets[0] = instance with
        {
            Positions = instance.Positions.Select(p => p.MinViewportWidth == 992 ? p with { MinHeight = 700, Stretch = false, Size = new DashboardWidgetSize(p.Size.ColumnSpan, 2) } : p).ToArray()
        };
        await Save(state);
        var stored = (await State(layer)).Layout.Widgets.Single();
        Assert.That(stored.GetPosition(992).MinHeight, Is.EqualTo(700));
        Assert.That(stored.GetPosition(992).Stretch, Is.False);
        Assert.That(stored.GetPosition(992).Size.RowSpan, Is.EqualTo(2));
        Assert.That(stored.GetPosition(0).Stretch, Is.True);
        var rendered = await _service.PrepareDashboardAsync((await State(layer)).Layout, _customer, false);
        string css = new DashboardCssBuilder().Build(rendered);
        Assert.That(css, Does.Contain("align-self: start"));
        Assert.That(css, Does.Contain("align-self: stretch"));
        Assert.That(stored.GetPosition(0).MinHeight, Is.Null);
        if (layer == DashboardLayoutLayer.User)
            Assert.That((await State(DashboardLayoutLayer.Global)).Layout.Widgets.Single().GetPosition(992).MinHeight, Is.Null);
    }

    /// <summary>
    /// Saves legitimate placement changes while rejecting client-supplied grid metadata and ownership.
    /// </summary>
    [TestCase(DashboardLayoutLayer.User)]
    [TestCase(DashboardLayoutLayer.Global)]
    public async Task Saving_Replaces_Untrusted_Metadata_Without_Discarding_Placements(DashboardLayoutLayer layer)
    {
        var state = await State(layer);
        var trusted = DefaultLayout();
        state.Layout.Scope = layer == DashboardLayoutLayer.User ? DashboardLayoutScope.Global : DashboardLayoutScope.User;
        state.Layout.CustomerId = -1;
        state.Layout.ColumnCount = 1;
        state.Layout.GridTemplateColumns = "untrusted";
        state.Layout.GridAutoRows = "untrusted";
        state.Layout.ColumnGap = "untrusted";
        state.Layout.RowGap = "untrusted";
        state.Layout.Version = -1;
        var instance = state.Layout.Widgets.Single();
        state.Layout.Widgets[0] = instance with
        {
            Positions = instance.Positions.Select(position => position.MinViewportWidth == 0 ? position with { Row = 2 } : position).ToArray()
        };

        await Save(state);
        var stored = (await State(layer)).Layout;
        Assert.Multiple(() =>
        {
            Assert.That(stored.Scope, Is.EqualTo(layer == DashboardLayoutLayer.User ? DashboardLayoutScope.User : DashboardLayoutScope.Global));
            Assert.That(stored.CustomerId, Is.EqualTo(layer == DashboardLayoutLayer.User ? _customer.Id : 0));
            Assert.That(stored.ColumnCount, Is.EqualTo(trusted.ColumnCount));
            Assert.That(stored.GridTemplateColumns, Is.EqualTo(trusted.GridTemplateColumns));
            Assert.That(stored.GridAutoRows, Is.EqualTo(trusted.GridAutoRows));
            Assert.That(stored.ColumnGap, Is.EqualTo(trusted.ColumnGap));
            Assert.That(stored.RowGap, Is.EqualTo(trusted.RowGap));
            Assert.That(stored.Version, Is.EqualTo(trusted.Version));
            Assert.That(stored.Widgets.Single().GetPosition(0).Row, Is.EqualTo(2));
            Assert.That(stored.Widgets.Single().GetPosition(1200), Is.EqualTo(instance.GetPosition(1200)));
        });
    }


}

#nullable enable

/// <summary>
/// Verifies cached global layouts, detached drafts and file change invalidation.
/// </summary>
[TestFixture]
public class DashboardLayoutCachingTests
{
    private MemoryCache _cache = null!;
    private CancellationTokenSource _changes = null!;
    private Mock<IFileSystem> _files = null!;
    private TestDashboardService _service = null!;
    private string? _json;
    private int _reads;

    /// <summary>
    /// Creates an isolated cache with controllable file contents and change notifications.
    /// </summary>
    [SetUp]
    public void CreateService()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _changes = new CancellationTokenSource();
        _json = Serialize(7);
        _reads = 0;
        var file = new Mock<IFile>();
        file.SetupGet(x => x.Exists).Returns(() => _json != null);
        file.Setup(x => x.OpenReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                _reads++;
                return new MemoryStream(Encoding.UTF8.GetBytes(_json ?? ""));
            });
        _files = new Mock<IFileSystem>();
        _files.Setup(x => x.Watch(It.IsAny<string>())).Returns(() => new CancellationChangeToken(_changes.Token));
        _files.Setup(x => x.FileExists(It.IsAny<string>())).Returns(() => _json != null);
        _files.Setup(x => x.GetFileAsync(It.IsAny<string>())).ReturnsAsync(file.Object);
        var app = new Mock<IApplicationContext>();
        app.SetupGet(x => x.TenantRoot).Returns(_files.Object);
        _service = new TestDashboardService(app.Object, _cache, NullEventPublisher.Instance, [],
            [new Lazy<IDashboardLayoutProvider, DashboardMetadata>(
                () => new TestLayoutProvider(new DashboardLayout("admin-dashboard")),
                new DashboardMetadata { SystemName = "admin-dashboard" })]);
    }

    /// <summary>
    /// Releases the cache and file notification registrations.
    /// </summary>
    [TearDown]
    public void ReleaseCache()
    {
        _cache.Dispose();
        _changes.Dispose();
    }

    private static string Serialize(int revision)
        => JsonSerializer.Serialize(new DashboardLayout("admin-dashboard") { Revision = revision }, SmartJsonOptions.CamelCased);

    private void SignalChange()
    {
        using var previous = _changes;
        _changes = new CancellationTokenSource();
        previous.Cancel();
    }

    private ValueTask<DashboardLayoutState> State()
        => _service.GetLayoutStateAsync("admin-dashboard", null, DashboardLayoutLayer.Global);

    /// <summary>
    /// The shared layout service does not impose an admin-specific schema version.
    /// </summary>
    [TestCase(1)]
    [TestCase(99)]
    public async Task Stored_Schema_Version_Is_Preserved(int version)
    {
        _json = JsonSerializer.Serialize(new DashboardLayout("admin-dashboard") { Version = version }, SmartJsonOptions.CamelCased);
        var state = await State();
        Assert.Multiple(() =>
        {
            Assert.That(state.Source, Is.EqualTo(DashboardLayoutLayer.Global));
            Assert.That(state.Layout.Version, Is.EqualTo(version));
        });
    }

    /// <summary>
    /// Repeated reads reuse the file snapshot while each caller receives a detached layout.
    /// </summary>
    [Test]
    public async Task Reuses_Global_File_Without_Sharing_Mutable_Drafts()
    {
        var first = await State();
        first.Layout.ColumnGap = "3rem";
        var second = await State();

        Assert.Multiple(() =>
        {
            Assert.That(_reads, Is.EqualTo(1));
            Assert.That(second.Layout.ColumnGap, Is.Not.EqualTo("3rem"));
        });
        _files.Verify(x => x.Watch("dashboard.admin-dashboard.json"), Times.Once);
    }

    /// <summary>
    /// A file change notification invalidates the cached layout.
    /// </summary>
    [Test]
    public async Task Invalidates_Layout_When_File_Changes()
    {
        var first = await State();
        _json = Serialize(8);
        var cached = await State();
        Assert.Multiple(() =>
        {
            Assert.That(cached.Layout.Revision, Is.EqualTo(7));
            Assert.That(_reads, Is.EqualTo(1));
        });

        SignalChange();
        var updated = await State();
        Assert.Multiple(() =>
        {
            Assert.That(updated.Layout.Revision, Is.EqualTo(8));
            Assert.That(_reads, Is.EqualTo(2));
        });
    }

    /// <summary>
    /// Creation, deletion and repair of an unusable shared file invalidate the cached fallback too.
    /// </summary>
    [TestCase(null, true)]
    [TestCase("", true)]
    [TestCase("{ invalid json }", true)]
    [TestCase(null, false)]
    public async Task File_Changes_Invalidate_Cached_Fallbacks(string? initialJson, bool create)
    {
        _json = create ? initialJson : Serialize(7);
        var initial = await State();
        _json = create ? Serialize(8) : null;
        SignalChange();
        var updated = await State();

        Assert.Multiple(() =>
        {
            Assert.That(initial.Source, Is.EqualTo(create ? DashboardLayoutLayer.System : DashboardLayoutLayer.Global));
            Assert.That(updated.Source, Is.EqualTo(create ? DashboardLayoutLayer.Global : DashboardLayoutLayer.System));
            Assert.That(updated.Layout.Revision, Is.EqualTo(create ? 8 : 0));
        });
    }
}
