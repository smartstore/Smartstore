#nullable enable

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentValidation;
using Smartstore.Core.Security;
using Smartstore.Json;

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
    protected DashboardViewComponentWidget(
        IPermissionService permissions,
        string requiredPermission)
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
        => new ComponentWidget<TComponent>(GetWidgetArguments(context, instance)) { Key = instance.Id };

    /// <summary>Creates the arguments passed to the widget view component.</summary>
    protected virtual object? GetWidgetArguments(DashboardWidgetContext context, DashboardWidgetInstance instance)
        => null;

    /// <inheritdoc />
    public virtual Task<Widget?> CreateConfigurationWidgetAsync(
        DashboardWidgetContext context,
        DashboardWidgetInstance instance)
        => Task.FromResult<Widget?>(null);

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

/// <summary>
/// Loads and validates typed widget settings and creates the configuration surface.
/// </summary>
/// <typeparam name="TComponent">The MVC view component used to render the widget.</typeparam>
/// <typeparam name="TSettings">The widget's settings model.</typeparam>
public abstract class DashboardViewComponentWidget<TComponent, TSettings> : DashboardViewComponentWidget<TComponent>
    where TComponent : Microsoft.AspNetCore.Mvc.ViewComponent
    where TSettings : class, new()
{
    private static readonly JsonSerializerOptions _settingsJsonOptions = new(SmartJsonOptions.CamelCased)
    {
        NumberHandling = JsonNumberHandling.Strict
    };

    private readonly IValidator<TSettings> _settingsValidator;

    /// <summary>Initializes a widget with its settings validator.</summary>
    protected DashboardViewComponentWidget(
        IPermissionService permissions,
        string requiredPermission,
        IValidator<TSettings> settingsValidator)
        : base(permissions, requiredPermission)
    {
        _settingsValidator = Guard.NotNull(settingsValidator);
    }

    /// <summary>Deserializes persisted settings into the widget's model.</summary>
    protected virtual TSettings LoadSettings(JsonObject settings)
    {
        Guard.NotNull(settings);
        try
        {
            return settings.Deserialize<TSettings>(_settingsJsonOptions) ?? new TSettings();
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid dashboard widget settings.", nameof(settings), ex);
        }
    }

    /// <inheritdoc />
    public override async ValueTask ValidateSettingsAsync(JsonObject settings, CancellationToken cancelToken = default)
    {
        Guard.NotNull(settings);
        var model = LoadSettings(settings);
        var result = await _settingsValidator.ValidateAsync(model, cancelToken);

        if (!result.IsValid)
        {
            throw new ArgumentException(result.ToString(), nameof(settings));
        }
    }

    /// <inheritdoc />
    protected override object? GetWidgetArguments(DashboardWidgetContext context, DashboardWidgetInstance instance)
        => GetWidgetArguments(context, instance, LoadSettings(instance.Settings));

    /// <summary>Creates component arguments from the typed settings.</summary>
    protected abstract object? GetWidgetArguments(DashboardWidgetContext context, DashboardWidgetInstance instance, TSettings settings);
}
