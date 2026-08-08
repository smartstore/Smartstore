#nullable enable

using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Widgets;
using Smartstore.Core.Widgets.Dashboard;

namespace Smartstore.Core.Tests.Platform.Widgets;

/// <summary>
/// Provides a deterministic dashboard widget implementation for service tests.
/// </summary>
internal sealed class TestDashboardWidget : IDashboardWidget
{
    /// <summary>
    /// Identifies the dashboard widget type used by the tests.
    /// </summary>
    public const string SystemName = "Tests.Dashboard.Widget";

    /// <inheritdoc />
    public DashboardWidgetDescriptor Descriptor { get; } = new(SystemName, "Tests.Dashboard.Widget.Title")
    {
        SettingsVersion = 2,
        DefaultSize = new DashboardWidgetSize(4),
        MinSize = new DashboardWidgetSize(2),
        MaxSize = new DashboardWidgetSize(12),
        Policy = new DashboardWidgetPolicy
        {
            AllowResize = false
        }
    };

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(
        DashboardWidgetContext context,
        CancellationToken cancelToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public Widget CreateWidget(DashboardWidgetContext context, DashboardWidgetInstance instance)
        => new HtmlWidget("test") { Key = instance.Id };

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
    {
        settings["migrated"] = true;
        return ValueTask.FromResult(settings);
    }
}
