#nullable enable

using Smartstore.Admin.Components;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine.Modularity;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that renders the Smartstore news feed.
/// </summary>
[SystemName(SystemName)]
public sealed class NewsFeedDashboardWidget : DashboardViewComponentWidget<NewsFeedViewComponent>
{
    /// <summary>
    /// Identifies the news feed dashboard widget type.
    /// </summary>
    public const string SystemName = "Smartstore.Admin.Dashboard.NewsFeed";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, ResolvableText.Resource("Admin.NewsFeed.Title"))
    {
        Description = ResolvableText.Resource("Admin.NewsFeed.Title"),
        Group = KnownDashboardWidgetGroups.System,
        IconName = "newspaper",
        CssClass = "news-feed",
        Order = 800,
        DefaultSize = new DashboardWidgetSize(2, 4),
        MinSize = new DashboardWidgetSize(2, 1),
        MaxSize = new DashboardWidgetSize(12, 6),
        AllowedSizes =
        [
            new DashboardWidgetSize(2, 4),
            new DashboardWidgetSize(2, 6),
            new DashboardWidgetSize(12, 1)
        ]
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;
}
