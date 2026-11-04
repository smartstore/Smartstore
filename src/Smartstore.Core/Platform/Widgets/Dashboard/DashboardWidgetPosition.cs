#nullable enable

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Describes a responsive grid position that applies from <see cref="MinViewportWidth"/> upwards.
/// </summary>
public sealed record DashboardWidgetPosition
{
    /// <summary>
    /// The maximum requested minimum height in pixels accepted for a widget position.
    /// </summary>
    public const int MaxRequestedHeight = 10000;

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

    /// <summary>
    /// Gets the optional minimum widget height in pixels. The shared grid row may grow further to fit its content.
    /// </summary>
    public int? MinHeight { get; init; }

    /// <summary>
    /// Gets whether the widget fills its grid area or keeps its content and requested minimum height.
    /// </summary>
    public bool Stretch { get; init; } = true;
}
