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
            var position = item.Instance.Positions.Single(x => x.MinViewportWidth == 0);
            AppendPosition(builder, model.Layout.BuildWidgetId(item.Instance.Id), position, 0);
        }

        var breakpoints = model.Widgets
            .SelectMany(x => x.Instance.Positions)
            .Where(x => x.MinViewportWidth > 0)
            .Select(x => x.MinViewportWidth)
            .Distinct()
            .Order()
            .ToArray();

        foreach (var breakpoint in breakpoints)
        {
            builder.Append("@media screen and (min-width: ")
                .Append(breakpoint.ToString(CultureInfo.InvariantCulture))
                .AppendLine("px) {");

            foreach (var item in model.Widgets)
            {
                var position = item.Instance.Positions.FirstOrDefault(x => x.MinViewportWidth == breakpoint);
                if (position != null)
                {
                    AppendPosition(builder, model.Layout.BuildWidgetId(item.Instance.Id), position, 1);
                }
            }

            builder.AppendLine("}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Appends one grid placement rule to a dashboard stylesheet.
    /// </summary>
    /// <param name="builder">The stylesheet builder.</param>
    /// <param name="widgetId">The dashboard-qualified, CSS-safe widget instance identifier.</param>
    /// <param name="position">The grid position to emit.</param>
    /// <param name="indentLevel">The indentation level of the generated rule.</param>
    private static void AppendPosition(
        StringBuilder builder,
        string widgetId,
        DashboardWidgetPosition position,
        int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        var columnStart = position.Column + 1;
        var rowStart = position.Row + 1;

        builder.Append(indent)
            .Append('#').Append(widgetId)
            .Append(" { grid-column: ")
            .Append(columnStart.ToString(CultureInfo.InvariantCulture))
            .Append(" / span ")
            .Append(position.Size.ColumnSpan.ToString(CultureInfo.InvariantCulture))
            .Append("; grid-row: ")
            .Append(rowStart.ToString(CultureInfo.InvariantCulture));

        if (position.Size.RowSpan.HasValue)
        {
            builder.Append(" / span ")
                .Append(position.Size.RowSpan.Value.ToString(CultureInfo.InvariantCulture));
        }

        builder.AppendLine("; }");
    }
}
