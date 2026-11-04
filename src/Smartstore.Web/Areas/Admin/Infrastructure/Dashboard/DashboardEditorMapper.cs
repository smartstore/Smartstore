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
    private const int MaxEditorViewportWidth = 10000;
    private const int MaxEditorRow = 10000;
    private const int MaxEditorRowSpan = 1000;

    private static readonly JsonSerializerOptions _changesJsonOptions = new(SmartJsonOptions.CamelCased)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    /// <summary>
    /// Projects an authorized dashboard view into the minimal browser contract.
    /// </summary>
    public static DashboardEditorData CreateData(DashboardEditorModel model, string editUrl, string cancelUrl, string previewUrl)
    {
        var movableIds = model.Dashboard.Widgets.Where(x => x.Policy.AllowMove).Select(x => x.Instance.Id).ToHashSet();
        var layout = model.State.Layout;
        return new DashboardEditorData(
            new DashboardEditorLayout(layout.Id, layout.ColumnCount,
                layout.Widgets.Select(x => new DashboardEditorWidget(x.Id, x.WidgetSystemName, x.Positions, movableIds.Contains(x.Id))).ToArray()),
            model.State.Layer.ToString(), model.IsDirty, model.Reset, editUrl, cancelUrl, previewUrl,
            model.Catalog.Select(x => new DashboardEditorWidgetChoice(x.SystemName, x.AllowMultipleInstances, x.MinSize, x.MaxSize, x.AllowedSizes)).ToArray());
    }

    /// <summary>
    /// Reads only the explicitly supported client changes, rejecting server-owned or unknown fields.
    /// </summary>
    public static DashboardLayoutChanges ReadChanges(string json)
    {
        var changes = JsonSerializer.Deserialize<DashboardLayoutChanges>(json ?? "null", _changesJsonOptions);
        if (changes?.Widgets == null)
            throw new ArgumentException("Invalid dashboard changes.");
        return changes;
    }

    /// <summary>
    /// Applies template placements to the supplied target draft while retaining its metadata and widget restrictions.
    /// </summary>
    public static DashboardLayout ApplyTemplate(DashboardLayout source, DashboardLayout target,
        IReadOnlyCollection<DashboardWidgetDescriptor> availableWidgets, IDashboardService dashboardService)
    {
        var descriptors = availableWidgets.ToDictionary(x => x.SystemName, StringComparer.OrdinalIgnoreCase);
        var existing = target.Widgets.ToDictionary(x => x.Id);
        var placements = new List<DashboardWidgetPlacement>();
        foreach (var widget in source.Widgets)
        {
            var positions = widget.Positions;
            if (existing.TryGetValue(widget.Id, out var previous))
            {
                if (!descriptors.TryGetValue(previous.WidgetSystemName, out var descriptor))
                {
                    placements.Add(new(previous.Id, previous.WidgetSystemName, previous.Positions));
                    continue;
                }

                var policy = DashboardWidgetPolicy.Combine(descriptor.Policy, previous.Policy);
                positions = widget.Positions.Concat(previous.Positions).Select(x => x.MinViewportWidth).Distinct().OrderBy(x => x)
                    .Select(width =>
                    {
                        var desired = widget.GetPosition(width);
                        var original = previous.GetPosition(width);
                        return desired with
                        {
                            MinViewportWidth = width,
                            Column = policy.AllowMove ? desired.Column : original.Column,
                            Row = policy.AllowMove ? desired.Row : original.Row,
                            MinHeight = policy.AllowResize ? desired.MinHeight : original.MinHeight,
                            Stretch = policy.AllowResize ? desired.Stretch : original.Stretch,
                            Size = new DashboardWidgetSize(
                                policy.AllowResize ? desired.Size.ColumnSpan : original.Size.ColumnSpan,
                                policy.AllowResize ? desired.Size.RowSpan : original.Size.RowSpan)
                        };
                    }).ToArray();
            }
            if (descriptors.ContainsKey(widget.WidgetSystemName))
                placements.Add(new(widget.Id, widget.WidgetSystemName, positions));
        }
        var includedIds = placements.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var widget in target.Widgets.Where(x => !includedIds.Contains(x.Id)))
        {
            if (!descriptors.TryGetValue(widget.WidgetSystemName, out var descriptor) ||
                DashboardWidgetPolicy.Combine(descriptor.Policy, widget.Policy).IsRequired)
                placements.Add(new(widget.Id, widget.WidgetSystemName, widget.Positions));
        }
        return ApplyChanges(new(placements), target, dashboardService);
    }

    /// <summary>
    /// Applies proposed placements to a detached server layout, preserving existing metadata and creating new instances from registered defaults.
    /// </summary>
    public static DashboardLayout ApplyChanges(DashboardLayoutChanges changes, DashboardLayout baseline, IDashboardService dashboardService)
    {
        var existing = baseline.Widgets.ToDictionary(x => x.Id);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var widgets = new List<DashboardWidgetInstance>();
        int nextOrder = baseline.Widgets.Select(x => x.Order).DefaultIfEmpty(0).Max();
        foreach (var placement in changes.Widgets)
        {
            if (placement == null || string.IsNullOrWhiteSpace(placement.Id) || placement.Id != placement.Id.SanitizeHtmlId() ||
                string.IsNullOrWhiteSpace(placement.WidgetSystemName) || !ids.Add(placement.Id) ||
                placement.Positions == null || placement.Positions.Count == 0 || placement.Positions.Any(x => x?.Size == null))
                throw new ArgumentException("Invalid widget placement.");

            if (placement.Positions.Any(position => position.MinViewportWidth > MaxEditorViewportWidth ||
                position.Row > MaxEditorRow || position.Size.RowSpan > MaxEditorRowSpan || position.MinHeight is < 0 or > DashboardWidgetPosition.MaxRequestedHeight))
                throw new ArgumentException("Widget placement exceeds the dashboard editor limits.");

            if (existing.TryGetValue(placement.Id, out var instance))
            {
                if (placement.WidgetSystemName != instance.WidgetSystemName)
                    throw new ArgumentException("An existing widget cannot change its type.");
            }
            else
            {
                var widget = dashboardService.GetWidget(placement.WidgetSystemName);
                instance = CreateWidgetInstance(widget, placement.Id, nextOrder = checked(nextOrder + 100));
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
        var news = layout.Widgets.FirstOrDefault(widget => widget.WidgetSystemName == NewsFeedDashboardWidget.SystemName);
        return layout.Widgets.SelectMany(widget => widget.Positions).Select(position => position.MinViewportWidth)
            .Append(0).Append(768).Append(992).Distinct().OrderBy(width => width)
            .Select(width =>
            {
                int columns = width >= 992 && news != null ? news.GetPosition(width).Column : layout.ColumnCount;
                var widgetSize = new DashboardWidgetSize(width < 768 ? layout.ColumnCount : size.ColumnSpan, size.RowSpan ?? 1);
                var occupied = layout.Widgets.Where(widget => widget != news).Select(widget => widget.GetPosition(width)).ToArray();
                // The first available row is either zero or the bottom edge of an existing rectangle.
                var rows = occupied.Select(position => position.Row + (position.Size.RowSpan ?? 1)).Append(0).Distinct().Order();
                foreach (int row in rows)
                {
                    for (int column = 0; column + widgetSize.ColumnSpan <= columns; column++)
                    {
                        bool overlaps = occupied.Any(position =>
                            column < position.Column + position.Size.ColumnSpan && column + widgetSize.ColumnSpan > position.Column &&
                            row < position.Row + (position.Size.RowSpan ?? 1) && row + widgetSize.RowSpan.Value > position.Row);
                        if (!overlaps)
                        {
                            return new DashboardWidgetPosition { MinViewportWidth = width, Row = row, Column = column, Size = widgetSize };
                        }
                    }
                }
                throw new InvalidOperationException("The widget does not fit into the dashboard's available columns.");
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
            Settings = widget.CreateDefaultSettings()
        };

}
