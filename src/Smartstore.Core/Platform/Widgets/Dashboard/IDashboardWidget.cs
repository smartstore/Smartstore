#nullable enable

using System.Text.Json.Nodes;

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Describes, validates and creates one kind of dashboard widget.
/// </summary>
public interface IDashboardWidget
{
    /// <summary>
    /// Gets the metadata and intrinsic capabilities of the widget type.
    /// </summary>
    DashboardWidgetDescriptor Descriptor { get; }

    /// <summary>
    /// Determines whether the widget is available for the current dashboard request.
    /// </summary>
    /// <param name="context">The current dashboard context.</param>
    /// <param name="cancelToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> when the widget is available; otherwise, <see langword="false"/>.</returns>
    ValueTask<bool> IsAvailableAsync(DashboardWidgetContext context, CancellationToken cancelToken = default);

    /// <summary>
    /// Creates the renderable widget for a concrete dashboard instance.
    /// </summary>
    /// <param name="context">The current dashboard context.</param>
    /// <param name="instance">The concrete widget instance.</param>
    /// <returns>The renderable widget.</returns>
    Widget CreateWidget(DashboardWidgetContext context, DashboardWidgetInstance instance);

    /// <summary>
    /// Creates the optional renderable widget used to configure a concrete dashboard instance.
    /// </summary>
    /// <param name="context">The current dashboard context.</param>
    /// <param name="instance">The concrete widget instance.</param>
    /// <returns>The configuration widget, or <see langword="null"/> when the widget has no configuration UI.</returns>
    Widget? CreateConfigurationWidget(DashboardWidgetContext context, DashboardWidgetInstance instance);

    /// <summary>
    /// Creates a fresh settings payload for a new widget instance.
    /// </summary>
    /// <returns>The default settings payload.</returns>
    JsonObject CreateDefaultSettings();

    /// <summary>
    /// Validates a widget settings payload at the current schema version.
    /// </summary>
    /// <param name="settings">The settings payload to validate.</param>
    /// <param name="cancelToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous validation operation.</returns>
    ValueTask ValidateSettingsAsync(JsonObject settings, CancellationToken cancelToken = default);

    /// <summary>
    /// Migrates a settings payload from an earlier schema version to the current version.
    /// </summary>
    /// <param name="settings">The settings payload to migrate.</param>
    /// <param name="fromVersion">The schema version of the supplied payload.</param>
    /// <param name="cancelToken">A token to cancel the operation.</param>
    /// <returns>The settings payload migrated to <see cref="DashboardWidgetDescriptor.SettingsVersion"/>.</returns>
    ValueTask<JsonObject> MigrateSettingsAsync(
        JsonObject settings,
        int fromVersion,
        CancellationToken cancelToken = default);
}
