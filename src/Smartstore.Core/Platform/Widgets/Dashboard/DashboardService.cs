#nullable enable

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Smartstore.Core.Identity;
using Smartstore.Events;
using Smartstore.IO;
using Smartstore.Json;

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Provides dashboard registration, layered layout resolution and render-model preparation.
/// </summary>
public class DashboardService : IDashboardService
{
    /// <summary>
    /// Defines the prefix used for customer-specific dashboard layout attributes.
    /// </summary>
    private const string UserLayoutAttributePrefix = "DashboardLayout.";

    private readonly IApplicationContext _appContext;
    private readonly IMemoryCache _memCache;
    private readonly IEventPublisher _eventPublisher;
    private readonly IEnumerable<Lazy<IDashboardWidget, DashboardMetadata>> _widgetRegistrations;
    private readonly IEnumerable<Lazy<IDashboardLayoutProvider, DashboardMetadata>> _layoutProviderRegistrations;
    private IReadOnlyDictionary<string, Lazy<IDashboardWidget, DashboardMetadata>>? _widgets;
    private IReadOnlyDictionary<string, Lazy<IDashboardLayoutProvider, DashboardMetadata>>? _layoutProviders;
    private IReadOnlyCollection<DashboardWidgetDescriptor>? _descriptors;

    public DashboardService(
        IApplicationContext appContext,
        IMemoryCache memCache,
        IEventPublisher eventPublisher,
        IEnumerable<Lazy<IDashboardWidget, DashboardMetadata>> widgetRegistrations,
        IEnumerable<Lazy<IDashboardLayoutProvider, DashboardMetadata>> layoutProviderRegistrations)
    {
        _appContext = appContext;
        _memCache = memCache;
        _eventPublisher = eventPublisher;
        _widgetRegistrations = widgetRegistrations;
        _layoutProviderRegistrations = layoutProviderRegistrations;
    }

    private IReadOnlyDictionary<string, Lazy<IDashboardWidget, DashboardMetadata>> Widgets
        => _widgets ??= CreateWidgetMap();

    private IReadOnlyDictionary<string, Lazy<IDashboardLayoutProvider, DashboardMetadata>> LayoutProviders
        => _layoutProviders ??= CreateLayoutProviderMap();

    /// <summary>
    /// Gets or sets the property-injected diagnostic logger.
    /// </summary>
    public ILogger Logger { get; set; } = NullLogger.Instance;

    /// <summary>
    /// Serializes a dashboard layout using the canonical JSON settings.
    /// </summary>
    /// <param name="layout">The dashboard layout to serialize.</param>
    /// <returns>The serialized dashboard layout.</returns>
    protected virtual string SerializeLayout(DashboardLayout layout)
    {
        Guard.NotNull(layout);

        return JsonSerializer.Serialize(layout, SmartJsonOptions.CamelCased);
    }

    /// <summary>
    /// Deserializes a dashboard layout using the canonical JSON settings.
    /// </summary>
    /// <param name="json">The serialized dashboard layout.</param>
    /// <returns>The deserialized dashboard layout, or <see langword="null"/> for a JSON null value.</returns>
    protected virtual DashboardLayout? DeserializeLayout(string json)
    {
        Guard.NotEmpty(json);

        return JsonSerializer.Deserialize<DashboardLayout>(json, SmartJsonOptions.CamelCased);
    }

    public IReadOnlyCollection<DashboardWidgetDescriptor> GetDescriptors()
        => _descriptors ??= CreateDescriptors();

    public IDashboardWidget GetWidget(string systemName)
    {
        Guard.NotEmpty(systemName);

        if (!Widgets.TryGetValue(systemName, out var registration))
        {
            throw new KeyNotFoundException($"Dashboard widget '{systemName}' is not registered.");
        }

        return ResolveWidget(registration);
    }

    public async ValueTask<DashboardLayout> GetEffectiveLayoutAsync(
        string dashboardId,
        Customer? customer,
        CancellationToken cancelToken = default)
    {
        Guard.NotEmpty(dashboardId);

        if (!LayoutProviders.TryGetValue(dashboardId, out var providerRegistration))
        {
            throw new KeyNotFoundException($"Dashboard '{dashboardId}' is not registered.");
        }

        var canonicalDashboardId = providerRegistration.Metadata.SystemName;
        var layout = LoadUserLayout(canonicalDashboardId, customer);

        if (layout == null)
        {
            layout = await LoadGlobalLayoutAsync(canonicalDashboardId, cancelToken);
        }

        if (layout == null)
        {
            var provider = providerRegistration.Value;
            layout = Guard.NotNull(provider.GetDefaultLayout());

            await _eventPublisher.PublishAsync(new DashboardLayoutBuiltEvent(layout), cancelToken);

            if (layout.Scope != DashboardLayoutScope.Global || layout.CustomerId != 0)
            {
                throw new InvalidOperationException(
                    $"System-default layout for dashboard '{canonicalDashboardId}' must be global.");
            }

            ValidateLayout(layout, canonicalDashboardId, customer);
        }

        return layout;
    }

    public virtual async ValueTask<DashboardRenderModel> GetDashboardAsync(
        string dashboardId,
        Customer? customer,
        bool isEditMode = false,
        CancellationToken cancelToken = default)
    {
        var layout = await GetEffectiveLayoutAsync(dashboardId, customer, cancelToken);
        var context = new DashboardWidgetContext
        {
            DashboardId = layout.Id,
            Customer = customer,
            IsEditMode = isEditMode
        };

        var items = new List<DashboardRenderItem>(layout.Widgets.Count);
        var singletonWidgets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var instance in layout.Widgets.OrderBy(x => x.Order))
        {
            if (!Widgets.TryGetValue(instance.WidgetSystemName, out var widgetRegistration))
            {
                if (instance.Policy.IsRequired)
                {
                    throw new InvalidOperationException(
                        $"Required dashboard widget '{instance.WidgetSystemName}' is not registered.");
                }

                continue;
            }

            var dashboardWidget = ResolveWidget(widgetRegistration);
            var descriptor = dashboardWidget.Descriptor;
            if (!descriptor.AllowMultipleInstances && !singletonWidgets.Add(descriptor.SystemName))
            {
                throw new InvalidOperationException(
                    $"Dashboard widget '{descriptor.SystemName}' does not allow multiple instances.");
            }

            if (!await dashboardWidget.IsAvailableAsync(context, cancelToken))
            {
                continue;
            }

            var settings = (JsonObject)instance.Settings.DeepClone();
            var settingsVersion = instance.SettingsVersion;

            if (settingsVersion != descriptor.SettingsVersion)
            {
                settings = Guard.NotNull(await dashboardWidget.MigrateSettingsAsync(
                    settings,
                    settingsVersion,
                    cancelToken));

                settingsVersion = descriptor.SettingsVersion;
            }

            await dashboardWidget.ValidateSettingsAsync(settings, cancelToken);

            var runtimeInstance = instance with
            {
                Settings = settings,
                SettingsVersion = settingsVersion
            };

            var policy = DashboardWidgetPolicy.Combine(descriptor.Policy, runtimeInstance.Policy);

            items.Add(new DashboardRenderItem
            {
                Instance = runtimeInstance,
                Descriptor = descriptor,
                Policy = policy,
                Widget = dashboardWidget.CreateWidget(context, runtimeInstance)
            });
        }

        return new DashboardRenderModel
        {
            Layout = layout,
            Context = context,
            Widgets = items
        };
    }

    private IReadOnlyDictionary<string, Lazy<IDashboardWidget, DashboardMetadata>> CreateWidgetMap()
    {
        var widgets = new Dictionary<string, Lazy<IDashboardWidget, DashboardMetadata>>(StringComparer.OrdinalIgnoreCase);

        foreach (var registration in _widgetRegistrations)
        {
            var systemName = registration.Metadata.SystemName;

            if (!widgets.TryAdd(systemName, registration))
            {
                throw new InvalidOperationException(
                    $"Dashboard widget '{systemName}' is registered more than once.");
            }
        }

        return widgets;
    }

    private IReadOnlyDictionary<string, Lazy<IDashboardLayoutProvider, DashboardMetadata>> CreateLayoutProviderMap()
    {
        var providers = new Dictionary<string, Lazy<IDashboardLayoutProvider, DashboardMetadata>>(StringComparer.OrdinalIgnoreCase);

        foreach (var registration in _layoutProviderRegistrations)
        {
            var systemName = registration.Metadata.SystemName;

            if (!providers.TryAdd(systemName, registration))
            {
                throw new InvalidOperationException(
                    $"Dashboard layout provider for '{systemName}' is registered more than once.");
            }
        }

        return providers;
    }

    private IReadOnlyCollection<DashboardWidgetDescriptor> CreateDescriptors()
    {
        return Widgets.Values
            .Select(ResolveWidget)
            .Select(x => x.Descriptor)
            .OrderBy(x => x.Order)
            .ThenBy(x => x.SystemName, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    private static IDashboardWidget ResolveWidget(Lazy<IDashboardWidget, DashboardMetadata> registration)
    {
        var widget = registration.Value;
        ValidateDescriptor(widget.Descriptor);

        if (!widget.Descriptor.SystemName.EqualsNoCase(registration.Metadata.SystemName))
        {
            throw new InvalidOperationException(
                $"Dashboard widget metadata '{registration.Metadata.SystemName}' does not match descriptor '{widget.Descriptor.SystemName}'.");
        }

        return widget;
    }

    /// <summary>
    /// Loads and validates a customer-specific layout from a generic attribute.
    /// </summary>
    /// <param name="dashboardId">The canonical dashboard identifier.</param>
    /// <param name="customer">The customer whose layout should be loaded.</param>
    /// <returns>The valid user layout, or <see langword="null"/> when no usable layout exists.</returns>
    private DashboardLayout? LoadUserLayout(string dashboardId, Customer? customer)
    {
        if (customer == null || customer.IsTransientRecord())
        {
            return null;
        }

        var attributeKey = UserLayoutAttributePrefix + dashboardId;
        var json = customer.GenericAttributes.Get<string>(attributeKey);

        if (json.IsEmpty())
        {
            return null;
        }

        try
        {
            var layout = DeserializeLayout(json);
            return ValidateCustomLayout(layout, dashboardId, customer, DashboardLayoutScope.User, "user");
        }
        catch (JsonException exception)
        {
            Logger.Error(
                exception,
                $"Failed to load user layout for dashboard '{dashboardId}' and customer '{customer.Id}'.");

            return null;
        }
    }

    /// <summary>
    /// Loads and validates a global layout from <c>TenantRoot/dashboard.{id}.json</c>.
    /// </summary>
    /// <param name="dashboardId">The canonical dashboard identifier.</param>
    /// <param name="cancelToken">A token to cancel the operation.</param>
    /// <returns>The valid global layout, or <see langword="null"/> when no usable file exists.</returns>
    private async Task<DashboardLayout?> LoadGlobalLayoutAsync(string dashboardId, CancellationToken cancelToken)
    {
        cancelToken.ThrowIfCancellationRequested();

        var fileName = $"dashboard.{dashboardId}.json";
        var fileSystem = _appContext.TenantRoot;
        var cacheKey = _memCache.BuildScopedKey($"DashboardLayout:{dashboardId}");

        var layout = await _memCache.GetOrCreateAsync<DashboardLayout?>(cacheKey, async entry =>
        {
            entry.ExpirationTokens.Add(fileSystem.Watch(fileName) ?? NullChangeToken.Singleton);

            if (!fileSystem.FileExists(fileName))
            {
                return null;
            }

            try
            {
                var json = await fileSystem.ReadAllTextAsync(fileName);
                if (json.IsEmpty())
                {
                    Logger.Warn($"Ignored empty global layout file '{fileName}' for dashboard '{dashboardId}'.");
                    return null;
                }

                var candidate = DeserializeLayout(json);
                return ValidateCustomLayout(candidate, dashboardId, null, DashboardLayoutScope.Global, "global");
            }
            catch (JsonException ex)
            {
                Logger.Error(ex, $"Failed to load global layout for dashboard '{dashboardId}' from '{fileName}'.");
                return null;
            }
            catch (Exception ex) when (
                ex is IOException or UnauthorizedAccessException or FileSystemException)
            {
                Logger.Error(ex, $"Failed to read global layout for dashboard '{dashboardId}' from '{fileName}'.");
                return null;
            }
        });

        cancelToken.ThrowIfCancellationRequested();
        return layout;
    }

    /// <summary>
    /// Validates a custom layout and converts validation failures into a layer fallback.
    /// </summary>
    /// <param name="layout">The custom layout to validate.</param>
    /// <param name="dashboardId">The canonical dashboard identifier.</param>
    /// <param name="customer">The expected customer.</param>
    /// <param name="scope">The expected layout scope.</param>
    /// <param name="layerName">The layer name used for diagnostics.</param>
    /// <returns>The valid layout, or <see langword="null"/> when validation fails.</returns>
    private DashboardLayout? ValidateCustomLayout(
        DashboardLayout? layout,
        string dashboardId,
        Customer? customer,
        DashboardLayoutScope scope,
        string layerName)
    {
        var expectedCustomerId = scope == DashboardLayoutScope.Global ? 0 : customer?.Id ?? 0;

        if (layout == null || layout.Scope != scope || layout.CustomerId != expectedCustomerId)
        {
            Logger.Warn($"Ignored {layerName} layout for dashboard '{dashboardId}' because its scope or customer assignment is invalid.");
            return null;
        }

        try
        {
            ValidateLayout(layout, dashboardId, customer);
            return layout;
        }
        catch (InvalidOperationException ex)
        {
            Logger.Error(ex, $"Ignored invalid {layerName} layout for dashboard '{dashboardId}'.");
            return null;
        }
    }

    /// <summary>
    /// Validates the invariants of a registered widget descriptor.
    /// </summary>
    /// <param name="descriptor">The descriptor to validate.</param>
    /// <exception cref="InvalidOperationException">The descriptor violates a dashboard widget invariant.</exception>
    private static void ValidateDescriptor(DashboardWidgetDescriptor descriptor)
    {
        Guard.NotNull(descriptor);

        if (descriptor.SettingsVersion <= 0)
        {
            throw new InvalidOperationException(
                $"Dashboard widget '{descriptor.SystemName}' must declare a positive settings version.");
        }

        var defaultColumns = descriptor.DefaultSize.ColumnSpan;
        if (defaultColumns < descriptor.MinSize.ColumnSpan || defaultColumns > descriptor.MaxSize.ColumnSpan)
        {
            throw new InvalidOperationException(
                $"The default size of dashboard widget '{descriptor.SystemName}' is outside its declared bounds.");
        }
    }

    /// <summary>
    /// Validates the identity, scope, grid definition and widget instances of a resolved layout.
    /// </summary>
    /// <param name="layout">The resolved layout to validate.</param>
    /// <param name="requestedDashboardId">The dashboard identifier requested from the provider.</param>
    /// <param name="customer">The customer for whom the layout was requested.</param>
    /// <exception cref="InvalidOperationException">The layout violates a dashboard layout invariant.</exception>
    private static void ValidateLayout(DashboardLayout layout, string requestedDashboardId, Customer? customer)
    {
        if (!layout.Id.EqualsNoCase(requestedDashboardId))
        {
            throw new InvalidOperationException(
                $"Dashboard layout provider returned layout '{layout.Id}' for requested dashboard '{requestedDashboardId}'.");
        }

        if (layout.Version <= 0 || layout.ColumnCount <= 0 || layout.GridTemplateColumns.IsEmpty())
        {
            throw new InvalidOperationException($"Dashboard layout '{layout.Id}' has an invalid grid definition.");
        }

        if (layout.Scope == DashboardLayoutScope.Global && layout.CustomerId != 0)
        {
            throw new InvalidOperationException($"Global dashboard layout '{layout.Id}' cannot be assigned to a customer.");
        }

        if (layout.Scope == DashboardLayoutScope.User && (layout.CustomerId <= 0 || layout.CustomerId != customer?.Id))
        {
            throw new InvalidOperationException(
                $"User dashboard layout '{layout.Id}' is not assigned to customer '{customer?.Id}'.");
        }

        if (layout.Widgets is null)
            throw new InvalidOperationException($"Dashboard layout '{layout.Id}' has no widget collection.");

        var instanceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var instance in layout.Widgets)
        {
            if (instance is null || instance.Id.IsEmpty() || instance.WidgetSystemName.IsEmpty())
                throw new InvalidOperationException($"Dashboard layout '{layout.Id}' contains an invalid widget instance.");

            if (!instanceIds.Add(instance.Id))
            {
                throw new InvalidOperationException(
                    $"Dashboard layout '{layout.Id}' contains duplicate widget instance ID '{instance.Id}'.");
            }

            if (instance.Settings is null || instance.Policy is null || instance.Positions is null)
                throw new InvalidOperationException($"Dashboard widget instance '{instance.Id}' is incomplete.");

            if (instance.Positions.Count == 0 || !instance.Positions.Any(x => x?.MinViewportWidth == 0))
            {
                throw new InvalidOperationException(
                    $"Dashboard widget instance '{instance.Id}' must declare a base position.");
            }

            if (instance.SettingsVersion <= 0)
            {
                throw new InvalidOperationException(
                    $"Dashboard widget instance '{instance.Id}' must declare a positive settings version.");
            }

            if (instance.Positions.GroupBy(x => x.MinViewportWidth).Any(x => x.Count() > 1))
            {
                throw new InvalidOperationException(
                    $"Dashboard widget instance '{instance.Id}' declares a viewport breakpoint more than once.");
            }

            foreach (var position in instance.Positions)
            {
                if (position?.Size is null)
                    throw new InvalidOperationException($"Dashboard widget instance '{instance.Id}' contains an invalid grid position.");

                if (position.MinViewportWidth < 0 || position.Column < 0 || position.Row < 0 ||
                    position.Column + position.Size.ColumnSpan > layout.ColumnCount)
                {
                    throw new InvalidOperationException(
                        $"Dashboard widget instance '{instance.Id}' contains an invalid grid position.");
                }
            }
        }
    }
}
