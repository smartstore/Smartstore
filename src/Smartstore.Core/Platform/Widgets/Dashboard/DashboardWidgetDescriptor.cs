#nullable enable

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Describes a dashboard widget type independently from its concrete layout instances.
/// </summary>
public sealed class DashboardWidgetDescriptor
{
    /// <summary>
    /// Initializes a new dashboard widget descriptor.
    /// </summary>
    /// <param name="systemName">The stable and globally unique widget identifier.</param>
    /// <param name="titleResKey">The localization resource key for the widget title.</param>
    public DashboardWidgetDescriptor(string systemName, string titleResKey)
    {
        Guard.NotEmpty(systemName);
        Guard.NotEmpty(titleResKey);

        SystemName = systemName;
        TitleResKey = titleResKey;
    }

    /// <summary>
    /// Gets the stable and globally unique widget identifier.
    /// </summary>
    public string SystemName { get; }

    /// <summary>
    /// Gets the localization resource key for the widget title.
    /// </summary>
    public string TitleResKey { get; }

    /// <summary>
    /// Gets the optional localization resource key for the widget description.
    /// </summary>
    public string? DescriptionResKey { get; init; }

    /// <summary>
    /// Gets the optional localization resource key for the widget category.
    /// </summary>
    public string? CategoryResKey { get; init; }

    /// <summary>
    /// Gets the optional name of the Bootstrap icon used to render the widget.
    /// </summary>
    public string? IconName { get; init; }

    /// <summary>
    /// Gets the optional CSS class used for widget-specific styling.
    /// </summary>
    public string? CssClass { get; init; }

    /// <summary>
    /// Gets the default ordering priority of the widget type.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Gets a value indicating whether a dashboard may contain multiple instances of this widget type.
    /// </summary>
    public bool AllowMultipleInstances { get; init; }

    /// <summary>
    /// Gets a value indicating whether the widget supports independent refresh operations.
    /// </summary>
    public bool SupportsRefresh { get; init; }

    /// <summary>
    /// Gets the optional default interval for automatic widget refreshes.
    /// </summary>
    public TimeSpan? DefaultRefreshInterval { get; init; }

    /// <summary>
    /// Gets the current schema version of the widget settings.
    /// </summary>
    public int SettingsVersion { get; init; } = 1;

    /// <summary>
    /// Gets the default widget size used when a new instance is created.
    /// </summary>
    public required DashboardWidgetSize DefaultSize { get; init; }

    /// <summary>
    /// Gets the smallest size supported by the widget.
    /// </summary>
    public required DashboardWidgetSize MinSize { get; init; }

    /// <summary>
    /// Gets the largest size supported by the widget.
    /// </summary>
    public required DashboardWidgetSize MaxSize { get; init; }

    /// <summary>
    /// Gets the optional discrete size presets supported by the widget.
    /// </summary>
    /// <remarks>An empty collection permits every size within the declared minimum and maximum bounds.</remarks>
    public IReadOnlyCollection<DashboardWidgetSize> AllowedSizes { get; init; } = [];

    /// <summary>
    /// Gets the intrinsic restrictions that no dashboard layout can loosen.
    /// </summary>
    public DashboardWidgetPolicy Policy { get; init; } = new();
}
