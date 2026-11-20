#nullable enable

using System.Globalization;
using System.Text;

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Builds responsive, dashboard-specific CSS rules for resolved widget placements.
/// </summary>
public sealed class DashboardCssBuilder : IDashboardCssBuilder
{
    /// <summary>
    /// Builds the responsive placement stylesheet for a resolved dashboard.
    /// </summary>
    /// <param name="model">The resolved dashboard render model.</param>
    /// <returns>The dashboard-specific CSS stylesheet.</returns>
    public string Build(DashboardRenderModel model)
    {
        Guard.NotNull(model);

        var builder = new StringBuilder();

        foreach (var item in model.Widgets)
        {
            if (item.Instance.Positions.Count == 0)
            {
                continue;
            }

            builder.Append('#').Append(model.Layout.BuildWidgetId(item.Instance.Id)).AppendLine(" {");

            DashboardWidgetPosition? previous = null;

            foreach (var position in item.Instance.Positions.OrderBy(position => position.Breakpoint))
            {
                AppendPosition(builder, position, previous);
                previous = position;
            }

            builder.AppendLine("}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Appends only the grid axes that differ from the preceding effective placement.
    /// </summary>
    private static void AppendPosition(
        StringBuilder builder,
        DashboardWidgetPosition position,
        DashboardWidgetPosition? previous)
    {
        var breakpoint = position.Breakpoint.ToString().ToLowerInvariant();

        if (previous == null || position.Column != previous.Column || position.Size.ColumnSpan != previous.Size.ColumnSpan)
        {
            builder.Append("    --dashboard-column-").Append(breakpoint).Append(": ")
                .Append((position.Column + 1).ToString(CultureInfo.InvariantCulture))
                .Append(" / span ")
                .Append(position.Size.ColumnSpan.ToString(CultureInfo.InvariantCulture))
                .AppendLine(";");
        }

        if (previous == null || position.Row != previous.Row || position.Size.RowSpan != previous.Size.RowSpan)
        {
            builder.Append("    --dashboard-row-").Append(breakpoint).Append(": ")
                .Append((position.Row + 1).ToString(CultureInfo.InvariantCulture));

            if (position.Size.RowSpan.HasValue)
            {
                builder.Append(" / span ")
                    .Append(position.Size.RowSpan.Value.ToString(CultureInfo.InvariantCulture));
            }

            builder.AppendLine(";");
        }
    }
}
