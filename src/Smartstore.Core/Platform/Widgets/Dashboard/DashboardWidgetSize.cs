#nullable enable

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Describes a widget size in dashboard grid units.
/// </summary>
public sealed record DashboardWidgetSize
{
    /// <summary>
    /// Initializes a new dashboard widget size.
    /// </summary>
    /// <param name="columnSpan">The number of grid columns occupied by the widget.</param>
    /// <param name="rowSpan">The optional number of grid rows occupied by the widget.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="columnSpan"/> or <paramref name="rowSpan"/> is less than one.
    /// </exception>
    public DashboardWidgetSize(int columnSpan, int? rowSpan = null)
    {
        if (columnSpan <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columnSpan));
        }

        if (rowSpan.HasValue && rowSpan.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowSpan));
        }

        ColumnSpan = columnSpan;
        RowSpan = rowSpan;
    }

    /// <summary>
    /// Gets the number of grid columns occupied by the widget.
    /// </summary>
    public int ColumnSpan { get; }

    /// <summary>
    /// Gets the number of grid rows occupied by the widget, or <see langword="null"/> when row sizing is automatic.
    /// </summary>
    public int? RowSpan { get; }
}
