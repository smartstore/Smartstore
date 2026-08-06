#nullable enable

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Supplies complete dashboard layouts and participates in effective layout resolution.
/// </summary>
public interface IDashboardLayoutProvider
{
    /// <summary>
    /// Gets the provider priority. Providers with a higher value are queried first.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Gets a layout for a dashboard and customer when this provider can supply one.
    /// </summary>
    /// <param name="dashboardId">The stable dashboard identifier.</param>
    /// <param name="customerId">The customer for whom the layout is requested.</param>
    /// <returns>The matching layout, or <see langword="null"/> to continue with the next provider.</returns>
    ValueTask<DashboardLayout?> GetLayoutAsync(
        string dashboardId,
        int customerId,
        CancellationToken cancelToken = default);
}
