#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Widgets.Dashboard;

namespace Smartstore.Core.Tests.Platform.Widgets;

/// <summary>
/// Returns a fixed dashboard layout with a configurable resolution priority.
/// </summary>
internal sealed class TestLayoutProvider : IDashboardLayoutProvider
{
    /// <summary>
    /// Stores the dashboard layout returned by the provider.
    /// </summary>
    private readonly DashboardLayout _layout;

    /// <summary>
    /// Initializes a new fixed dashboard layout provider.
    /// </summary>
    /// <param name="order">The resolution priority of the provider.</param>
    /// <param name="layout">The dashboard layout returned by the provider.</param>
    public TestLayoutProvider(int order, DashboardLayout layout)
    {
        Order = order;
        _layout = layout;
    }

    /// <inheritdoc />
    public int Order { get; }

    /// <inheritdoc />
    public ValueTask<DashboardLayout?> GetLayoutAsync(
        string dashboardId,
        int customerId,
        CancellationToken cancelToken = default)
        => ValueTask.FromResult<DashboardLayout?>(_layout);
}
