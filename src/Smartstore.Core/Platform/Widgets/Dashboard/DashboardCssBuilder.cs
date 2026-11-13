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
            foreach (var position in item.Instance.Positions.OrderBy(position => position.Breakpoint))
            {
                AppendPosition(builder, model.Layout.BuildWidgetId(item.Instance.Id), position);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Appends one grid placement rule to a dashboard stylesheet.
    /// </summary>
    /// <param name="builder">The stylesheet builder.</param>
    /// <param name="widgetId">The dashboard-qualified, CSS-safe widget instance identifier.</param>
    /// <param name="position">The grid position to emit.</param>
    private static void AppendPosition(
        StringBuilder builder,
        string widgetId,
        DashboardWidgetPosition position)
    {
        var breakpoint = position.Breakpoint.ToString().ToLowerInvariant();
        var columnStart = position.Column + 1;
        var rowStart = position.Row + 1;

        builder.Append('#').Append(widgetId)
            .Append(" { --dashboard-column-").Append(breakpoint).Append(": ")
            .Append(columnStart.ToString(CultureInfo.InvariantCulture))
            .Append(" / span ")
            .Append(position.Size.ColumnSpan.ToString(CultureInfo.InvariantCulture))
            .Append("; --dashboard-row-").Append(breakpoint).Append(": ")
            .Append(rowStart.ToString(CultureInfo.InvariantCulture));

        if (position.Size.RowSpan.HasValue)
        {
            builder.Append(" / span ")
                .Append(position.Size.RowSpan.Value.ToString(CultureInfo.InvariantCulture));
        }

        builder.AppendLine("; }");
    }
}
