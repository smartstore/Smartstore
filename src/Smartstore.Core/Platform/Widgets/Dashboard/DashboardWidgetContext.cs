#nullable enable

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Provides the request-wide inputs shared by all widgets rendered for one dashboard.
/// </summary>
public sealed class DashboardWidgetContext
{
    /// <summary>
    /// Gets the stable identifier of the dashboard being rendered.
    /// </summary>
    public required string DashboardId { get; init; }

    /// <summary>
    /// Gets the identifier of the customer for whom the dashboard is rendered.
    /// </summary>
    public int CustomerId { get; init; }

    /// <summary>
    /// Gets a value indicating whether the dashboard is rendered in edit mode.
    /// </summary>
    public bool IsEditMode { get; init; }
}
