#nullable enable

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Controls which dashboard customizations are permitted for a widget.
/// </summary>
public sealed record DashboardWidgetPolicy
{
    /// <summary>
    /// Gets a value indicating whether the widget must remain in the dashboard.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Gets a value indicating whether the widget may be moved.
    /// </summary>
    public bool AllowMove { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the widget may be resized.
    /// </summary>
    public bool AllowResize { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the widget settings may be changed.
    /// </summary>
    public bool AllowConfigure { get; init; }

    /// <summary>
    /// Gets a value indicating whether the widget may be refreshed independently.
    /// </summary>
    public bool AllowRefresh { get; init; }

    /// <summary>
    /// Combines the intrinsic widget policy with the policy of a concrete layout instance.
    /// </summary>
    /// <param name="widgetPolicy">The restrictions declared by the widget type.</param>
    /// <param name="layoutPolicy">The restrictions declared by the layout instance.</param>
    /// <returns>A policy containing the intersection of both sets of permitted operations.</returns>
    /// <remarks>
    /// A layout can further restrict capabilities, but cannot enable a capability that the widget itself does not support.
    /// </remarks>
    public static DashboardWidgetPolicy Combine(DashboardWidgetPolicy widgetPolicy, DashboardWidgetPolicy layoutPolicy)
    {
        Guard.NotNull(widgetPolicy);
        Guard.NotNull(layoutPolicy);

        return new DashboardWidgetPolicy
        {
            IsRequired = widgetPolicy.IsRequired || layoutPolicy.IsRequired,
            AllowMove = widgetPolicy.AllowMove && layoutPolicy.AllowMove,
            AllowResize = widgetPolicy.AllowResize && layoutPolicy.AllowResize,
            AllowConfigure = widgetPolicy.AllowConfigure && layoutPolicy.AllowConfigure,
            AllowRefresh = widgetPolicy.AllowRefresh && layoutPolicy.AllowRefresh
        };
    }
}
