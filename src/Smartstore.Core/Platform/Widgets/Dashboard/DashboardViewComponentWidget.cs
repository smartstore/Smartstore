#nullable enable

using System.Text.Json.Nodes;

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Provides default dashboard widget behavior for implementations backed by an MVC view component.
/// </summary>
/// <typeparam name="TComponent">The MVC view component used to render the widget.</typeparam>
public abstract class DashboardViewComponentWidget<TComponent> : IDashboardWidget
    where TComponent : Microsoft.AspNetCore.Mvc.ViewComponent
{
    /// <inheritdoc />
    public abstract DashboardWidgetDescriptor Descriptor { get; }

    /// <inheritdoc />
    public virtual ValueTask<bool> IsAvailableAsync(
        DashboardWidgetContext context,
        CancellationToken cancelToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public virtual Widget CreateWidget(DashboardWidgetContext context, DashboardWidgetInstance instance)
        => new ComponentWidget<TComponent> { Key = instance.Id };

    /// <inheritdoc />
    public virtual Widget? CreateConfigurationWidget(
        DashboardWidgetContext context,
        DashboardWidgetInstance instance)
        => null;

    /// <inheritdoc />
    public virtual JsonObject CreateDefaultSettings()
        => new();

    /// <inheritdoc />
    public virtual ValueTask ValidateSettingsAsync(JsonObject settings, CancellationToken cancelToken = default)
        => ValueTask.CompletedTask;

    /// <inheritdoc />
    public virtual ValueTask<JsonObject> MigrateSettingsAsync(
        JsonObject settings,
        int fromVersion,
        CancellationToken cancelToken = default)
        => ValueTask.FromResult(settings);
}
