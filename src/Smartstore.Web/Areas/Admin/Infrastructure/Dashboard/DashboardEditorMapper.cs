using System.Text.Json;
using System.Text.Json.Serialization;
using Smartstore.Admin.Models.Dashboard;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Json;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Maps between browser contracts and detached server layouts without accepting server metadata from clients.
/// </summary>
public static class DashboardEditorMapper
{
    private const int MaxEditorRow = 10000;
    private const int MaxEditorRowSpan = 1000;

    private static readonly JsonSerializerOptions _changesJsonOptions = new(SmartJsonOptions.CamelCased)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    /// <summary>
    /// Serializes an authorized dashboard view into the minimal JSON browser contract.
    /// </summary>
    public static string CreateJson(DashboardEditorModel model, string editUrl, string cancelUrl, string previewUrl)
    {
        var layout = new DashboardLayout(model.State.Layout.Id)
        {
            ColumnCount = model.State.Layout.ColumnCount,
            Widgets = model.Dashboard.Widgets.Select(widget => widget.Instance).ToList()
        };
        var movableIds = model.Dashboard.Widgets
            .Where(widget => widget.Policy.AllowMove)
            .Select(widget => widget.Instance.Id)
            .ToHashSet();

        var widgets = layout.Widgets.Select(widget =>
        {
            var settings = model.Dashboard.Widgets
                .FirstOrDefault(item => item.Instance.Id == widget.Id && item.Policy.AllowConfigure)
                ?.Instance.Settings;

            return new DashboardEditorWidget(
                widget.Id,
                widget.WidgetSystemName,
                widget.Positions,
                movableIds.Contains(widget.Id),
                settings);
        }).ToArray();

        var widgetChoices = model.Catalog
            .Select(descriptor => new DashboardEditorWidgetChoice(
                descriptor.SystemName,
                descriptor.AllowMultipleInstances,
                descriptor.MinSize,
                descriptor.MaxSize,
                descriptor.AllowedSizes))
            .ToArray();

        var editorLayout = new DashboardEditorLayout(layout.Id, layout.ColumnCount, widgets, GetLayoutRules(layout));
        var data = new DashboardEditorData(
            editorLayout,
            model.State.Layer.ToString(),
            model.IsDirty,
            model.Reset,
            editUrl,
            cancelUrl,
            previewUrl,
            widgetChoices);

        return JsonSerializer.Serialize(data, SmartJsonOptions.CamelCased);
    }

    /// <summary>
    /// Reads only the explicitly supported client changes, rejecting server-owned or unknown fields.
    /// </summary>
    public static DashboardLayoutChanges ReadChanges(string json)
    {
        var changes = JsonSerializer.Deserialize<DashboardLayoutChanges>(json ?? "null", _changesJsonOptions);
        Guard.Against<ArgumentException>(changes?.Widgets == null, "Invalid dashboard changes.");
        return changes;
    }

    /// <summary>
    /// Applies proposed placements to a detached server layout, preserving existing metadata and creating new instances from registered defaults.
    /// </summary>
    public static DashboardLayout ApplyChanges(
        DashboardLayoutChanges changes,
        DashboardLayout baseline,
        IDashboardService dashboardService)
    {
        var existing = baseline.Widgets.ToDictionary(x => x.Id);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var widgets = new List<DashboardWidgetInstance>();
        var nextOrder = baseline.Widgets.Select(x => x.Order).DefaultIfEmpty(0).Max();

        foreach (var placement in changes.Widgets)
        {
            Guard.Against<ArgumentException>(placement == null, "Invalid widget placement.");
            Guard.Against<ArgumentException>(
                !placement.Id.HasValue()
                || placement.Id != placement.Id.SanitizeHtmlId()
                || !placement.WidgetSystemName.HasValue()
                || !ids.Add(placement.Id),
                "Invalid widget placement.");

            Guard.Against<ArgumentException>(
                placement.Positions == null
                || placement.Positions.Count == 0
                || placement.Positions.Any(position => position?.Size == null),
                "Invalid widget placement.");

            var exceedsEditorLimits = placement.Positions.Any(position =>
                !Enum.IsDefined(position.Breakpoint)
                    || position.Row > MaxEditorRow
                    || position.Size.RowSpan > MaxEditorRowSpan);

            Guard.Against<ArgumentException>(exceedsEditorLimits, "Widget placement exceeds the dashboard editor limits.");

            if (existing.TryGetValue(placement.Id, out var instance))
            {
                Guard.Against<ArgumentException>(placement.WidgetSystemName != instance.WidgetSystemName, "An existing widget cannot change its type.");
            }
            else
            {
                var widget = dashboardService.GetWidget(placement.WidgetSystemName);
                nextOrder = checked(nextOrder + 100);
                instance = CreateWidgetInstance(widget, placement.Id, nextOrder);
            }
            if (placement.Settings != null)
            {
                var widget = dashboardService.GetWidget(instance.WidgetSystemName);

                if (!DashboardWidgetPolicy.Combine(widget.Descriptor.Policy, instance.Policy).AllowConfigure)
                {
                    throw new InvalidOperationException("This widget cannot be configured.");
                }
                    
                instance = instance with { Settings = (System.Text.Json.Nodes.JsonObject)placement.Settings.DeepClone() };
            }

            widgets.Add(instance with { Positions = placement.Positions });
        }

        baseline.Widgets = widgets;
        return baseline;
    }

    /// <summary>
    /// Finds the first free rectangle for each responsive layout, reserving the fixed news rail.
    /// </summary>
    internal static IReadOnlyCollection<DashboardWidgetPosition> GetInsertionPositions(DashboardLayout layout, DashboardWidgetSize size)
    {
        var newsWidget = layout.Widgets.FirstOrDefault(widget => widget.WidgetSystemName == NewsFeedDashboardWidget.SystemName);
        return GetLayoutRules(layout).Select(rule =>
        {
            var availableColumns = rule.ColumnCount;
            var columnSpan = rule.FullWidthWidgets ? availableColumns : size.ColumnSpan;
            var rowSpan = size.RowSpan ?? 1;
            var widgetSize = new DashboardWidgetSize(columnSpan, rowSpan);
            var occupiedPositions = layout.Widgets
                .Where(widget => widget != newsWidget)
                .Select(widget => widget.GetPosition(rule.Breakpoint))
                .ToArray();

            // The first available row is either zero or the bottom edge of an existing rectangle.
            var candidateRows = occupiedPositions
                .Select(position => position.Row + (position.Size.RowSpan ?? 1))
                .Append(0)
                .Distinct()
                .Order();

            foreach (var row in candidateRows)
            {
                for (var column = 0; column + columnSpan <= availableColumns; column++)
                {
                    var overlaps = occupiedPositions.Any(position =>
                    {
                        var overlapsColumns = column < position.Column + position.Size.ColumnSpan && column + columnSpan > position.Column;
                        var overlapsRows = row < position.Row + (position.Size.RowSpan ?? 1) && row + rowSpan > position.Row;

                        return overlapsColumns && overlapsRows;
                    });

                    if (!overlaps)
                    {
                        return new DashboardWidgetPosition
                        {
                            Breakpoint = rule.Breakpoint,
                            Row = row,
                            Column = column,
                            Size = widgetSize
                        };
                    }
                }
            }

            throw new InvalidOperationException("The widget does not fit into the dashboard's available columns.");
        }).ToArray();
    }

    internal static IReadOnlyCollection<DashboardEditorLayoutRule> GetLayoutRules(DashboardLayout layout)
    {
        var newsWidget = layout.Widgets.FirstOrDefault(widget => widget.WidgetSystemName == NewsFeedDashboardWidget.SystemName);
        return Enum.GetValues<DashboardBreakpoint>().Select(breakpoint =>
        {
            var hasNewsRail = breakpoint >= DashboardBreakpoint.Lg && newsWidget != null;
            var columns = hasNewsRail ? newsWidget.GetPosition(breakpoint).Column : layout.ColumnCount;
            return new DashboardEditorLayoutRule(breakpoint, columns, breakpoint < DashboardBreakpoint.Md, hasNewsRail);
        }).ToArray();
    }

    /// <summary>
    /// Creates an unsaved instance using the registered widget's settings and canonical system name.
    /// </summary>
    internal static DashboardWidgetInstance CreateWidgetInstance(IDashboardWidget widget, string id, int order)
        => new(id, widget.Descriptor.SystemName)
        {
            Order = order,
            SettingsVersion = widget.Descriptor.SettingsVersion,
            Settings = widget.CreateDefaultSettings(),
            Policy = new() { AllowConfigure = widget.Descriptor.Policy.AllowConfigure }
        };

}
