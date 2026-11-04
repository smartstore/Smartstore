using Smartstore.Core.Widgets.Dashboard;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Defines the start data consumed by the dashboard editor, independently of persisted layouts.
/// </summary>
/// <param name="Layout">The placements and grid width required for local editing.</param>
/// <param name="Layer">The authorized layer being edited.</param>
/// <param name="Dirty">Whether the server returned an unsaved draft.</param>
/// <param name="Reset">Whether removing the customization is pending.</param>
/// <param name="EditUrl">The route for changing the edited layer.</param>
/// <param name="CancelUrl">The route for leaving the editor.</param>
/// <param name="PreviewUrl">The route for previewing draft commands.</param>
/// <param name="Widgets">The available widget types and size choices.</param>
public sealed record DashboardEditorData(
    DashboardEditorLayout Layout,
    string Layer,
    bool Dirty,
    bool Reset,
    string EditUrl,
    string CancelUrl,
    string PreviewUrl,
    IReadOnlyCollection<DashboardEditorWidgetChoice> Widgets)
{
    /// <summary>
    /// Gets the maximum requested widget height accepted by the server.
    /// </summary>
    public int MaxHeight => DashboardWidgetPosition.MaxRequestedHeight;
}

/// <summary>
/// Contains only the layout information used by the browser's placement algorithm.
/// </summary>
/// <param name="Id">The dashboard's DOM identifier.</param>
/// <param name="ColumnCount">The number of grid columns.</param>
/// <param name="Widgets">The widget placements, including preserved placements of unavailable instances that do not occupy grid space.</param>
public sealed record DashboardEditorLayout(string Id, int ColumnCount, IReadOnlyCollection<DashboardEditorWidget> Widgets);

/// <summary>
/// Describes one editable placement without exposing widget settings or persisted metadata.
/// </summary>
/// <param name="Id">The instance identifier.</param>
/// <param name="WidgetSystemName">The widget type used to update the picker.</param>
/// <param name="Positions">The responsive placements.</param>
/// <param name="AllowMove">The effective move permission; the server rechecks it on submission.</param>
public sealed record DashboardEditorWidget(
    string Id,
    string WidgetSystemName,
    IReadOnlyCollection<DashboardWidgetPosition> Positions,
    bool AllowMove);

/// <summary>
/// Contains the widget catalog information used by duplicate detection.
/// </summary>
/// <param name="SystemName">The registered widget type.</param>
/// <param name="AllowMultipleInstances">Whether more than one instance may be added.</param>
/// <param name="MinSize">The minimum permitted widget size.</param>
/// <param name="MaxSize">The maximum permitted widget size.</param>
/// <param name="AllowedSizes">The optional explicit size restrictions.</param>
public sealed record DashboardEditorWidgetChoice(
    string SystemName,
    bool AllowMultipleInstances,
    DashboardWidgetSize MinSize,
    DashboardWidgetSize MaxSize,
    IReadOnlyCollection<DashboardWidgetSize> AllowedSizes);

/// <summary>
/// Defines the complete proposed widget membership and placements submitted by the editor.
/// </summary>
/// <param name="Widgets">The instances to retain or add; omitted instances are proposed removals.</param>
public sealed record DashboardLayoutChanges(IReadOnlyCollection<DashboardWidgetPlacement> Widgets);

/// <summary>
/// Contains the only widget values the editor may submit for persistence or preview.
/// </summary>
/// <param name="Id">The stable instance identifier.</param>
/// <param name="WidgetSystemName">The registered type; existing instances cannot change type.</param>
/// <param name="Positions">The proposed responsive placements.</param>
public sealed record DashboardWidgetPlacement(
    string Id,
    string WidgetSystemName,
    IReadOnlyCollection<DashboardWidgetPosition> Positions);
