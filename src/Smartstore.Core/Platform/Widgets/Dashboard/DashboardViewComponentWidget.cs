#nullable enable

using System.Text.Json.Nodes;
using Smartstore.Core.Security;

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Provides default dashboard widget behavior for implementations backed by an MVC view component.
/// </summary>
/// <typeparam name="TComponent">The MVC view component used to render the widget.</typeparam>
public abstract class DashboardViewComponentWidget<TComponent> : IDashboardWidget
    where TComponent : Microsoft.AspNetCore.Mvc.ViewComponent
{
    private readonly IPermissionService _permissionService;
    private readonly string _requiredPermission;

    /// <summary>
    /// Initializes a widget with the widget permission required by its view component.
    /// </summary>
    /// <param name="requiredPermission">The widget permission declared by the view component.</param>
    protected DashboardViewComponentWidget(IPermissionService permissions, string requiredPermission)
    {
        Guard.NotNull(permissions);
        Guard.NotEmpty(requiredPermission);
        _permissionService = permissions;
        _requiredPermission = requiredPermission;
    }

    /// <inheritdoc />
    public abstract DashboardWidgetDescriptor Descriptor { get; }

    /// <inheritdoc />
    public virtual ValueTask<bool> IsAvailableAsync(
        DashboardWidgetContext context,
        CancellationToken cancelToken = default)
        => new ValueTask<bool>(_permissionService.AuthorizeAsync(_requiredPermission, context.Customer));

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
