#nullable enable

using Smartstore.Core.Identity;

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
    /// Gets the customer for whom the dashboard is rendered.
    /// </summary>
    public Customer? Customer { get; init; }

    /// <summary>
    /// Gets a value indicating whether the dashboard is rendered in edit mode.
    /// </summary>
    public bool IsEditMode { get; init; }
}
