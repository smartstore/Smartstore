#nullable enable

using System.Text.Json.Nodes;
using Smartstore.Core.Widgets;
using Smartstore.Core.Widgets.Dashboard;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that renders the store statistics partial view.
/// </summary>
public sealed class StoreReportDashboardWidget : IDashboardWidget
{
    /// <summary>
    /// Identifies the store report dashboard widget type.
    /// </summary>
    public const string SystemName = "Smartstore.Admin.Dashboard.StoreReport";

    /// <summary>
    /// Identifies the partial view rendered by the widget.
    /// </summary>
    public const string ViewPath = "~/Areas/Admin/Views/Store/StoreDashboardReport.cshtml";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, "Admin.Report.StoreStatistics")
    {
        DescriptionResKey = "Admin.Report.StoreStatistics",
        CategoryResKey = "Admin.Dashboard.StoreStatistics",
        IconName = "bar-chart",
        CssClass = "report-store-statistics",
        Order = 700,
        DefaultSize = new DashboardWidgetSize(4, 1),
        MinSize = new DashboardWidgetSize(4, 1),
        MaxSize = new DashboardWidgetSize(12, 1),
        AllowedSizes =
        [
            new DashboardWidgetSize(4, 1),
            new DashboardWidgetSize(10, 1),
            new DashboardWidgetSize(12, 1)
        ]
    };

    /// <inheritdoc />
    public DashboardWidgetDescriptor Descriptor => _descriptor;

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(
        DashboardWidgetContext context,
        CancellationToken cancelToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public Widget CreateWidget(DashboardWidgetContext context, DashboardWidgetInstance instance)
        => new PartialViewWidget(ViewPath) { Key = instance.Id };

    /// <inheritdoc />
    public Widget? CreateConfigurationWidget(
        DashboardWidgetContext context,
        DashboardWidgetInstance instance)
        => null;

    /// <inheritdoc />
    public JsonObject CreateDefaultSettings()
        => new();

    /// <inheritdoc />
    public ValueTask ValidateSettingsAsync(JsonObject settings, CancellationToken cancelToken = default)
        => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask<JsonObject> MigrateSettingsAsync(
        JsonObject settings,
        int fromVersion,
        CancellationToken cancelToken = default)
        => ValueTask.FromResult(settings);
}
