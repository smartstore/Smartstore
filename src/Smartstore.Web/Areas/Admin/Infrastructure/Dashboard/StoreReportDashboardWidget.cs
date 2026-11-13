#nullable enable

using System.Text.Json.Nodes;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Widgets;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine.Modularity;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that renders the store statistics partial view.
/// </summary>
[SystemName(SystemName)]
public sealed class StoreReportDashboardWidget(IPermissionService permissions) : IDashboardWidget
{
    /// <summary>
    /// Identifies the store report dashboard widget type.
    /// </summary>
    public const string SystemName = "Smartstore.Admin.Dashboard.StoreReport";

    /// <summary>
    /// Identifies the partial view rendered by the widget.
    /// </summary>
    public const string ViewPath = "~/Areas/Admin/Views/Store/StoreDashboardReport.cshtml";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, ResolvableText.Resource("Admin.Report.StoreStatistics"))
    {
        Group = KnownDashboardWidgetGroups.System,
        IconName = "chart-bar",
        CssClass = "report-store-statistics",
        Order = 700,
        DefaultSize = new DashboardWidgetSize(4, 1),
        MinSize = new DashboardWidgetSize(3, 1),
        MaxSize = new DashboardWidgetSize(12)
    };

    /// <inheritdoc />
    public DashboardWidgetDescriptor Descriptor => _descriptor;

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(
        DashboardWidgetContext context,
        CancellationToken cancelToken = default)
        => new(permissions.AuthorizeAsync(Permissions.Dashboard.Widgets.System.StoreReport, context.Customer));

    /// <inheritdoc />
    public Widget CreateWidget(DashboardWidgetContext context, DashboardWidgetInstance instance)
        => new PartialViewWidget(ViewPath) { Key = instance.Id };

    /// <inheritdoc />
    public Task<Widget?> CreateConfigurationWidgetAsync(
        DashboardWidgetContext context,
        DashboardWidgetInstance instance)
        => Task.FromResult<Widget?>(null);

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
