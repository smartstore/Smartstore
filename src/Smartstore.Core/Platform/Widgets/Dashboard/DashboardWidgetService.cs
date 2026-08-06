#nullable enable

using System.Text.Json.Nodes;

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Provides the default registry, layout resolution and render-model preparation for dashboard widgets.
/// </summary>
public sealed class DashboardWidgetService : IDashboardWidgetService
{
    /// <summary>
    /// Contains registered widget implementations keyed by their case-insensitive system names.
    /// </summary>
    private readonly IReadOnlyDictionary<string, IDashboardWidget> _widgets;

    /// <summary>
    /// Contains layout providers ordered from highest to lowest priority.
    /// </summary>
    private readonly IDashboardLayoutProvider[] _layoutProviders;

    /// <summary>
    /// Contains widget descriptors in their declared display order.
    /// </summary>
    private readonly IReadOnlyCollection<DashboardWidgetDescriptor> _descriptors;

    /// <summary>
    /// Initializes a new dashboard widget service.
    /// </summary>
    /// <param name="widgets">The registered dashboard widget implementations.</param>
    /// <param name="layoutProviders">The registered dashboard layout providers.</param>
    /// <exception cref="InvalidOperationException">
    /// A widget descriptor is invalid or a widget system name is registered more than once.
    /// </exception>
    public DashboardWidgetService(
        IEnumerable<IDashboardWidget> widgets,
        IEnumerable<IDashboardLayoutProvider> layoutProviders)
    {
        var map = new Dictionary<string, IDashboardWidget>(StringComparer.OrdinalIgnoreCase);

        foreach (var widget in Guard.NotNull(widgets))
        {
            ValidateDescriptor(widget.Descriptor);

            if (!map.TryAdd(widget.Descriptor.SystemName, widget))
            {
                throw new InvalidOperationException(
                    $"Dashboard widget '{widget.Descriptor.SystemName}' is registered more than once.");
            }
        }

        _widgets = map;
        _descriptors = map.Values
            .Select(x => x.Descriptor)
            .OrderBy(x => x.Order)
            .ThenBy(x => x.SystemName, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();

        _layoutProviders = Guard.NotNull(layoutProviders)
            .OrderByDescending(x => x.Order)
            .ToArray();
    }

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

        foreach (var provider in _layoutProviders)
        {
            var layout = await provider.GetLayoutAsync(dashboardId, customerId, cancelToken);
            if (layout != null)
            {
                ValidateLayout(layout, dashboardId, customerId);
                return layout;
            }
        }

        throw new InvalidOperationException($"No layout for dashboard '{dashboardId}' was found.");
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
