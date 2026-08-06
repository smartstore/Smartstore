#nullable enable

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Describes a responsive grid position that applies from <see cref="MinViewportWidth"/> upwards.
/// </summary>
public sealed record DashboardWidgetPosition
{
    /// <summary>
    /// Gets the minimum viewport width in pixels. Zero represents the base layout.
    /// </summary>
    public int MinViewportWidth { get; init; }

    /// <summary>
    /// Gets the zero-based grid column at which the widget starts.
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// Gets the zero-based grid row at which the widget starts.
    /// </summary>
    public int Row { get; init; }

    /// <summary>
    /// Gets the size of the widget at this breakpoint.
    /// </summary>
    public required DashboardWidgetSize Size { get; init; }
}
