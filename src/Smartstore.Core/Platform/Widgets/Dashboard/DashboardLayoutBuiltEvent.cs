#nullable enable

using Smartstore.Events;

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Notifies consumers that a system-default dashboard layout has been built and can be modified.
/// </summary>
public sealed class DashboardLayoutBuiltEvent : IEventMessage
{
    public DashboardLayoutBuiltEvent(DashboardLayout layout)
    {
        Layout = Guard.NotNull(layout);
    }

    /// <summary>
    /// Gets the mutable system-default dashboard layout.
    /// </summary>
    public DashboardLayout Layout { get; }
}
