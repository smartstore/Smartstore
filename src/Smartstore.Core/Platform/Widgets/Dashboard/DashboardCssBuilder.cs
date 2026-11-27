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

            builder.AppendLine($"#{model.Layout.BuildWidgetId(item.Instance.Id)} {{");

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

        if (position.Hidden != (previous?.Hidden ?? false))
        {
            builder.AppendLine($"    --dashboard-display-{breakpoint}: {(position.Hidden ? "none" : "grid")};");
        }

        if (previous == null || position.Column != previous.Column || position.Size.ColumnSpan != previous.Size.ColumnSpan)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"    --dashboard-column-{breakpoint}: {position.Column + 1} / span {position.Size.ColumnSpan};");
        }

        if (previous == null || position.Row != previous.Row || position.Size.RowSpan != previous.Size.RowSpan)
        {
            builder.Append(CultureInfo.InvariantCulture, $"    --dashboard-row-{breakpoint}: {position.Row + 1}");

            if (position.Size.RowSpan.HasValue)
            {
                builder.Append(CultureInfo.InvariantCulture, $" / span {position.Size.RowSpan.Value}");
            }

            builder.AppendLine(";");
        }
    }
}
