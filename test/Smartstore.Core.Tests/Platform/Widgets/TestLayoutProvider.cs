#nullable enable

using Smartstore.Core.Widgets.Dashboard;

namespace Smartstore.Core.Tests.Platform.Widgets;

/// <summary>
/// Returns a fixed system-default dashboard layout.
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
    /// <param name="layout">The dashboard layout returned by the provider.</param>
    public TestLayoutProvider(DashboardLayout layout)
    {
        _layout = layout;
    }

    /// <inheritdoc />
    public string DashboardId => _layout.Id;

    /// <inheritdoc />
    public DashboardLayout GetDefaultLayout() => _layout;
}
