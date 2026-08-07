#nullable enable

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.IO;
using Smartstore.Json;

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Provides dashboard registration, layered layout resolution and render-model preparation.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    /// <summary>
    /// Defines the prefix used for customer-specific dashboard layout attributes.
    /// </summary>
    private const string UserLayoutAttributePrefix = "DashboardLayout.";

    /// <summary>
    /// Contains registered widget implementations keyed by their case-insensitive system names.
    /// </summary>
    private readonly IReadOnlyDictionary<string, IDashboardWidget> _widgets;

    /// <summary>
    /// Contains dashboard default providers keyed by their case-insensitive dashboard identifiers.
    /// </summary>
    private readonly IReadOnlyDictionary<string, IDashboardLayoutProvider> _layoutProviders;

    /// <summary>
    /// Contains widget descriptors in their declared display order.
    /// </summary>
    private readonly IReadOnlyCollection<DashboardWidgetDescriptor> _descriptors;

    /// <summary>
    /// Provides access to customer generic attributes.
    /// </summary>
    private readonly IGenericAttributeService _genericAttributeService;

    /// <summary>
    /// Provides access to the application data file system.
    /// </summary>
    private readonly IApplicationContext _applicationContext;

    /// <summary>
    /// Caches global layouts until their source files change.
    /// </summary>
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// Initializes a new dashboard service.
    /// </summary>
    /// <param name="widgets">The registered dashboard widget implementations.</param>
    /// <param name="layoutProviders">The registered dashboard default providers.</param>
    /// <param name="genericAttributeService">The generic attribute service.</param>
    /// <param name="applicationContext">The application context.</param>
    /// <param name="memoryCache">The application memory cache.</param>
    /// <exception cref="InvalidOperationException">
    /// A widget descriptor is invalid, or a widget system name or dashboard identifier is registered more than once.
    /// </exception>
    public DashboardService(
        IEnumerable<IDashboardWidget> widgets,
        IEnumerable<IDashboardLayoutProvider> layoutProviders,
        IGenericAttributeService genericAttributeService,
        IApplicationContext applicationContext,
        IMemoryCache memoryCache)
    {
        var widgetMap = new Dictionary<string, IDashboardWidget>(StringComparer.OrdinalIgnoreCase);

        foreach (var widget in Guard.NotNull(widgets))
        {
            ValidateDescriptor(widget.Descriptor);

            if (!widgetMap.TryAdd(widget.Descriptor.SystemName, widget))
            {
                throw new InvalidOperationException(
                    $"Dashboard widget '{widget.Descriptor.SystemName}' is registered more than once.");
            }
        }

        var providerMap = new Dictionary<string, IDashboardLayoutProvider>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in Guard.NotNull(layoutProviders))
        {
            Guard.NotEmpty(provider.DashboardId);

            if (!providerMap.TryAdd(provider.DashboardId, provider))
            {
                throw new InvalidOperationException(
                    $"Dashboard layout provider for '{provider.DashboardId}' is registered more than once.");
            }
        }

        _widgets = widgetMap;
        _layoutProviders = providerMap;
        _descriptors = widgetMap.Values
            .Select(x => x.Descriptor)
            .OrderBy(x => x.Order)
            .ThenBy(x => x.SystemName, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();

        _genericAttributeService = Guard.NotNull(genericAttributeService);
        _applicationContext = Guard.NotNull(applicationContext);
        _memoryCache = Guard.NotNull(memoryCache);
    }

    /// <summary>
    /// Gets or sets the property-injected diagnostic logger.
    /// </summary>
    public ILogger Logger { get; set; } = NullLogger.Instance;

    /// <inheritdoc />
    public IReadOnlyCollection<DashboardWidgetDescriptor> GetDescriptors()
        => _descriptors;

    /// <inheritdoc />
    public IDashboardWidget GetWidget(string systemName)
    {
        Guard.NotEmpty(systemName);

        if (!_widgets.TryGetValue(systemName, out var widget))
        {
            throw new KeyNotFoundException($"Dashboard widget '{systemName}' is not registered.");
        }

        return widget;
    }

    /// <inheritdoc />
    public async ValueTask<DashboardLayout> GetEffectiveLayoutAsync(
        string dashboardId,
        int customerId,
        CancellationToken cancelToken = default)
    {
        Guard.NotEmpty(dashboardId);

        if (!_layoutProviders.TryGetValue(dashboardId, out var provider))
        {
            throw new KeyNotFoundException($"Dashboard '{dashboardId}' is not registered.");
        }

        var canonicalDashboardId = provider.DashboardId;
        var layout = LoadUserLayout(canonicalDashboardId, customerId);

        if (layout == null)
        {
            layout = await LoadGlobalLayoutAsync(canonicalDashboardId, cancelToken);
        }

        if (layout == null)
        {
            layout = Guard.NotNull(provider.GetDefaultLayout());

            if (layout.Scope != DashboardLayoutScope.Global || layout.CustomerId != 0)
            {
                throw new InvalidOperationException(
                    $"System-default layout for dashboard '{canonicalDashboardId}' must be global.");
            }

            ValidateLayout(layout, canonicalDashboardId, customerId);
        }

        return layout;
    }

    /// <inheritdoc />
    public async ValueTask<DashboardRenderModel> GetDashboardAsync(
        string dashboardId,
        int customerId,
        bool isEditMode = false,
        CancellationToken cancelToken = default)
    {
        var layout = await GetEffectiveLayoutAsync(dashboardId, customerId, cancelToken);
        var context = new DashboardWidgetContext
        {
            DashboardId = layout.Id,
            CustomerId = customerId,
            IsEditMode = isEditMode
        };

        var items = new List<DashboardRenderItem>(layout.Widgets.Count);
        var singletonWidgets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var instance in layout.Widgets.OrderBy(x => x.Order))
        {
            if (!_widgets.TryGetValue(instance.WidgetSystemName, out var dashboardWidget))
            {
                if (instance.Policy.IsRequired)
                {
                    throw new InvalidOperationException(
                        $"Required dashboard widget '{instance.WidgetSystemName}' is not registered.");
                }

                continue;
            }

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
                Widget = Guard.NotNull(dashboardWidget.CreateWidget(context, runtimeInstance))
            });
        }

        return new DashboardRenderModel
        {
            Layout = layout,
            Context = context,
            Widgets = items
        };
    }

    /// <summary>
    /// Loads and validates a customer-specific layout from a generic attribute.
    /// </summary>
    /// <param name="dashboardId">The canonical dashboard identifier.</param>
    /// <param name="customerId">The customer whose layout should be loaded.</param>
    /// <returns>The valid user layout, or <see langword="null"/> when no usable layout exists.</returns>
    private DashboardLayout? LoadUserLayout(string dashboardId, int customerId)
    {
        if (customerId <= 0)
        {
            return null;
        }

        var attributeKey = UserLayoutAttributePrefix + dashboardId;
        var attributes = _genericAttributeService.GetAttributesForEntity(nameof(Customer), customerId);
        var json = attributes.Get<string>(attributeKey);

        if (json.IsEmpty())
        {
            return null;
        }

        try
        {
            var layout = JsonSerializer.Deserialize<DashboardLayout>(json, SmartJsonOptions.CamelCased);
            return ValidateCustomLayout(layout, dashboardId, customerId, DashboardLayoutScope.User, "user");
        }
        catch (JsonException exception)
        {
            Logger.Error(
                exception,
                "Failed to load user layout for dashboard '{DashboardId}' and customer '{CustomerId}'.",
                dashboardId,
                customerId);

            return null;
        }
    }

    /// <summary>
    /// Loads and validates a global layout from <c>App_Data/dashboard.{id}.json</c>.
    /// </summary>
    /// <param name="dashboardId">The canonical dashboard identifier.</param>
    /// <param name="cancelToken">A token to cancel the operation.</param>
    /// <returns>The valid global layout, or <see langword="null"/> when no usable file exists.</returns>
    private async Task<DashboardLayout?> LoadGlobalLayoutAsync(
        string dashboardId,
        CancellationToken cancelToken)
    {
        cancelToken.ThrowIfCancellationRequested();

        var fileName = $"dashboard.{dashboardId}.json";
        var fileSystem = _applicationContext.AppDataRoot;
        var cacheKey = _memoryCache.BuildScopedKey($"DashboardLayout:{dashboardId}");

        var layout = await _memoryCache.GetOrCreateAsync<DashboardLayout?>(cacheKey, async entry =>
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
                    Logger.Warn(
                        "Ignored empty global layout file '{FileName}' for dashboard '{DashboardId}'.",
                        fileName,
                        dashboardId);

                    return null;
                }

                var candidate = JsonSerializer.Deserialize<DashboardLayout>(json, SmartJsonOptions.CamelCased);
                return ValidateCustomLayout(candidate, dashboardId, 0, DashboardLayoutScope.Global, "global");
            }
            catch (JsonException exception)
            {
                Logger.Error(
                    exception,
                    "Failed to load global layout for dashboard '{DashboardId}' from '{FileName}'.",
                    dashboardId,
                    fileName);

                return null;
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or FileSystemException)
            {
                Logger.Error(
                    exception,
                    "Failed to read global layout for dashboard '{DashboardId}' from '{FileName}'.",
                    dashboardId,
                    fileName);

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
    /// <param name="customerId">The expected customer identifier.</param>
    /// <param name="scope">The expected layout scope.</param>
    /// <param name="layerName">The layer name used for diagnostics.</param>
    /// <returns>The valid layout, or <see langword="null"/> when validation fails.</returns>
    private DashboardLayout? ValidateCustomLayout(
        DashboardLayout? layout,
        string dashboardId,
        int customerId,
        DashboardLayoutScope scope,
        string layerName)
    {
        if (layout == null || layout.Scope != scope || layout.CustomerId != customerId)
        {
            Logger.Warn(
                "Ignored {LayerName} layout for dashboard '{DashboardId}' because its scope or customer assignment is invalid.",
                layerName,
                dashboardId);

            return null;
        }

        try
        {
            ValidateLayout(layout, dashboardId, customerId);
            return layout;
        }
        catch (InvalidOperationException exception)
        {
            Logger.Error(
                exception,
                "Ignored invalid {LayerName} layout for dashboard '{DashboardId}'.",
                layerName,
                dashboardId);

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
        if (defaultColumns < descriptor.MinimumSize.ColumnSpan || defaultColumns > descriptor.MaximumSize.ColumnSpan)
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
    /// <param name="customerId">The customer for whom the layout was requested.</param>
    /// <exception cref="InvalidOperationException">The layout violates a dashboard layout invariant.</exception>
    private static void ValidateLayout(DashboardLayout layout, string requestedDashboardId, int customerId)
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

        if (layout.Scope == DashboardLayoutScope.User &&
            (layout.CustomerId <= 0 || layout.CustomerId != customerId))
        {
            throw new InvalidOperationException(
                $"User dashboard layout '{layout.Id}' is not assigned to customer '{customerId}'.");
        }

        var instanceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var instance in layout.Widgets)
        {
            if (!instanceIds.Add(instance.Id))
            {
                throw new InvalidOperationException(
                    $"Dashboard layout '{layout.Id}' contains duplicate widget instance ID '{instance.Id}'.");
            }

            if (instance.Positions.Count == 0 || !instance.Positions.Any(x => x.MinViewportWidth == 0))
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
