#nullable enable

using Smartstore.Core.Identity;

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Contains registration metadata for a dashboard component.
/// </summary>
public sealed class DashboardMetadata
{
    /// <summary>
    /// Gets or sets the stable system name of the dashboard component.
    /// </summary>
    public string SystemName { get; set; } = string.Empty;
}

/// <summary>
/// Discovers dashboard widgets, resolves effective layouts and prepares dashboards for rendering.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Gets the descriptors of all registered dashboard widget types.
    /// </summary>
    /// <returns>The widget descriptors in their declared display order.</returns>
    IReadOnlyCollection<DashboardWidgetDescriptor> GetDescriptors();

    /// <summary>
    /// Gets a registered dashboard widget by its system name.
    /// </summary>
    /// <param name="systemName">The stable widget system name.</param>
    /// <returns>The registered dashboard widget.</returns>
    /// <exception cref="KeyNotFoundException">No widget is registered with the supplied system name.</exception>
    IDashboardWidget GetWidget(string systemName);

    /// <summary>
    /// Resolves the user, global or provider-default layout for a dashboard and customer.
    /// </summary>
    /// <param name="dashboardId">The stable dashboard identifier.</param>
    /// <param name="customer">The customer for whom the layout is requested.</param>
    /// <param name="cancelToken">A token to cancel the operation.</param>
    /// <returns>The validated effective layout.</returns>
    ValueTask<DashboardLayout> GetEffectiveLayoutAsync(
        string dashboardId,
        Customer? customer,
        CancellationToken cancelToken = default);

    /// <summary>
    /// Resolves, validates and prepares a dashboard for rendering.
    /// </summary>
    /// <param name="dashboardId">The stable dashboard identifier.</param>
    /// <param name="customer">The customer for whom the dashboard is rendered.</param>
    /// <param name="isEditMode">A value indicating whether the dashboard is rendered in edit mode.</param>
    /// <param name="cancelToken">A token to cancel the operation.</param>
    /// <returns>The fully resolved dashboard render model.</returns>
    ValueTask<DashboardRenderModel> GetDashboardAsync(
        string dashboardId,
        Customer? customer,
        bool isEditMode = false,
        CancellationToken cancelToken = default);
}
