using System.Text.Json.Nodes;
using Smartstore.Core.Identity;
using Smartstore.IO;

namespace Smartstore.Core.Widgets.Dashboard;

public partial class DashboardService
{
    /// <summary>
    /// Gets a customer's selected view without altering any customization.
    /// </summary>
    public DashboardLayoutLayer GetSelectedLayer(string dashboardId, Customer customer)
    {
        RequireLayer(dashboardId, DashboardLayoutLayer.User);
        var value = customer?.GenericAttributes.Get<string>("DashboardView." + dashboardId);
        return Enum.TryParse<DashboardLayoutLayer>(value, out var layer) && Enum.IsDefined(layer)
            ? layer : DashboardLayoutLayer.User;
    }

    /// <inheritdoc />
    public async Task SelectLayerAsync(
        string dashboardId,
        Customer customer,
        DashboardLayoutLayer layer,
        CancellationToken cancelToken = default)
    {
        RequireLayer(dashboardId, layer);
        Guard.NotNull(customer);
        customer.GenericAttributes.Set("DashboardView." + dashboardId, layer.ToString(), 0);
        await customer.GenericAttributes.SaveChangesAsync(cancelToken);
    }

    /// <inheritdoc />
    public async ValueTask<DashboardLayoutState> GetLayoutStateAsync(
        string dashboardId,
        Customer customer,
        DashboardLayoutLayer layer,
        bool reset = false,
        CancellationToken cancelToken = default)
    {
        RequireLayer(dashboardId, layer);
        cancelToken.ThrowIfCancellationRequested();
        var requestedLayer = reset
            ? (layer == DashboardLayoutLayer.User ? DashboardLayoutLayer.Global : DashboardLayoutLayer.System)
            : layer;
        var resolved = await ResolveLayoutAsync(dashboardId, customer, requestedLayer, cancelToken);
        return new DashboardLayoutState
        {
            Layout = DeserializeLayout(SerializeLayout(resolved.Layout)),
            Layer = layer,
            Source = resolved.Source
        };
    }

    /// <summary>
    /// Checks a registered dashboard identifier and selectable layer.
    /// </summary>
    private void RequireLayer(string dashboardId, DashboardLayoutLayer layer)
    {
        Guard.NotEmpty(dashboardId);

        if (!LayoutProviders.ContainsKey(dashboardId))
        {
            throw new KeyNotFoundException($"Dashboard '{dashboardId}' is not registered.");
        }

        if (!Enum.IsDefined(layer))
        {
            throw new ArgumentException("Unknown dashboard layout layer.", nameof(layer));
        }
    }

    /// <inheritdoc />
    public virtual void ApplyDraftMetadata(DashboardLayout draft, DashboardLayoutState state, Customer customer)
    {
        Guard.NotNull(draft);
        Guard.NotNull(state);
        Guard.NotNull(customer);
        RequireLayer(draft.Id, state.Layer);

        if (state.Layer == DashboardLayoutLayer.System || !draft.Id.EqualsNoCase(state.Layout.Id))
        {
            throw new ArgumentException("A draft requires a matching mutable dashboard state.");
        }

        // Grid expressions and ownership are trusted server metadata, never client input.
        draft.Scope = state.Layer == DashboardLayoutLayer.User ? DashboardLayoutScope.User : DashboardLayoutScope.Global;
        draft.CustomerId = state.Layer == DashboardLayoutLayer.User ? customer.Id : 0;
        draft.ColumnCount = state.Layout.ColumnCount;
        draft.GridTemplateColumns = state.Layout.GridTemplateColumns;
        draft.GridAutoRows = state.Layout.GridAutoRows;
        draft.ColumnGap = state.Layout.ColumnGap;
        draft.RowGap = state.Layout.RowGap;
    }

    /// <inheritdoc />
    public async Task ValidateDraftAsync(
        DashboardLayout layout,
        Customer customer,
        DashboardLayoutLayer layer,
        CancellationToken cancelToken = default)
    {
        RequireLayer(layout.Id, layer);

        if (layer == DashboardLayoutLayer.System)
        {
            throw new InvalidOperationException("The system default is read-only.");
        }

        var state = await GetLayoutStateAsync(layout.Id, customer, layer, cancelToken: cancelToken);
        await ValidateDraftAsync(layout, customer, state, cancelToken);
    }

    private async Task ValidateDraftAsync(
        DashboardLayout layout,
        Customer customer,
        DashboardLayoutState state,
        CancellationToken cancelToken)
    {
        var baseline = state.Layout;
        ApplyDraftMetadata(layout, state, customer);
        layout.Version = baseline.Version;
        ValidateLayout(layout, layout.Id, customer);

        var context = new DashboardWidgetContext { DashboardId = layout.Id, Customer = customer, IsEditMode = true };
        var remaining = baseline.Widgets.ToDictionary(x => x.Id, StringComparer.Ordinal);
        var singletons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < layout.Widgets.Count; i++)
        {
            var instance = layout.Widgets[i];
            remaining.Remove(instance.Id, out var previous);

            if (previous != null && instance.WidgetSystemName != previous.WidgetSystemName)
            {
                throw new InvalidOperationException("A widget instance cannot change its type.");
            }

            if (!Widgets.TryGetValue(instance.WidgetSystemName, out var registration))
            {
                throw new InvalidOperationException("Unknown widget.");
            }

            var widget = ResolveWidget(registration);
            var descriptor = widget.Descriptor;

            if (!descriptor.AllowMultipleInstances && !singletons.Add(descriptor.SystemName))
            {
                throw new InvalidOperationException("This widget only allows one instance.");
            }

            if (!await widget.IsAvailableAsync(context, cancelToken))
            {
                throw new InvalidOperationException("Widget is not available.");
            }

            if (previous != null)
            {
                var policy = DashboardWidgetPolicy.Combine(descriptor.Policy, previous.Policy);

                if (!policy.AllowConfigure && !JsonNode.DeepEquals(previous.Settings, instance.Settings))
                {
                    throw new InvalidOperationException("This widget cannot be configured.");
                }
            }

            layout.Widgets[i] = instance with { Policy = previous?.Policy ?? new() { AllowConfigure = descriptor.Policy.AllowConfigure } };

            foreach (var position in instance.Positions)
            {
                // Existing responsive placements can be narrower than the editor's resize presets.
                if (previous != null && previous.GetPosition(position.Breakpoint).Size == position.Size)
                {
                    continue;
                }

                var invalidWidth = position.Size.ColumnSpan < descriptor.MinSize.ColumnSpan
                    || position.Size.ColumnSpan > descriptor.MaxSize.ColumnSpan;
                var invalidHeight = position.Size.RowSpan is int rows
                    && ((descriptor.MinSize.RowSpan is int minRows && rows < minRows)
                        || (descriptor.MaxSize.RowSpan is int maxRows && rows > maxRows));
                var unsupportedSize = descriptor.AllowedSizes.Count > 0 && !descriptor.AllowedSizes.Contains(position.Size);

                if (invalidWidth || invalidHeight || unsupportedSize)
                {
                    throw new InvalidOperationException("Unsupported widget size.");
                }
            }

            if (instance.SettingsVersion != descriptor.SettingsVersion && (previous == null || instance.SettingsVersion != previous.SettingsVersion))
            {
                throw new InvalidOperationException("Unsupported widget settings version.");
            }

            await PrepareWidgetInstanceAsync(widget, instance, cancelToken);
        }

        // Instances left in the baseline were removed from the draft.
        foreach (var removed in remaining.Values)
        {
            if (!Widgets.TryGetValue(removed.WidgetSystemName, out var registration))
            {
                continue;
            }

            var widget = ResolveWidget(registration);

            if (!await widget.IsAvailableAsync(context, cancelToken))
            {
                continue;
            }

            if (DashboardWidgetPolicy.Combine(widget.Descriptor.Policy, removed.Policy).IsRequired)
            {
                throw new InvalidOperationException("A required widget cannot be removed.");
            }
        }
    }

    /// <inheritdoc />
    public async Task SaveLayoutAsync(
        DashboardLayout layout,
        Customer customer,
        DashboardLayoutLayer layer,
        bool reset = false,
        CancellationToken cancelToken = default)
    {
        Guard.NotNull(customer);
        RequireLayer(layout.Id, layer);

        if (layer == DashboardLayoutLayer.System)
        {
            throw new InvalidOperationException("The system default is read-only.");
        }

        cancelToken.ThrowIfCancellationRequested();
        string json = null;

        if (!reset)
        {
            var current = await GetLayoutStateAsync(layout.Id, customer, layer, cancelToken: cancelToken);
            await ValidateDraftAsync(layout, customer, current, cancelToken);
            layout.Revision = checked(current.Layout.Revision + 1);
            json = SerializeLayout(layout);
        }

        if (layer == DashboardLayoutLayer.User)
        {
            customer.GenericAttributes.Set(UserLayoutAttributePrefix + layout.Id, json, 0);
            await customer.GenericAttributes.SaveChangesAsync(cancelToken);
        }
        else
        {
            var path = _appContext.TenantRoot.MapPath($"dashboard.{layout.Id}.json");

            if (reset)
            {
                File.Delete(path);
            }
            else
            {
                var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    await File.WriteAllTextAsync(temporaryPath, json, cancelToken);
                    File.Move(temporaryPath, path, true);
                }
                finally
                {
                    File.Delete(temporaryPath);
                }
            }

            _memCache.Remove(_memCache.BuildScopedKey($"DashboardLayout:{layout.Id}"));
        }
    }
}
