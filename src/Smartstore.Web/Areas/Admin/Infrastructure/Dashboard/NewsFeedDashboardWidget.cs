#nullable enable

using Smartstore.Admin.Components;
using Smartstore.Core.Widgets.Dashboard;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that renders the Smartstore news feed.
/// </summary>
public sealed class NewsFeedDashboardWidget : DashboardViewComponentWidget<NewsFeedViewComponent>
{
    /// <summary>
    /// Identifies the news feed dashboard widget type.
    /// </summary>
    public const string SystemName = "Smartstore.Admin.Dashboard.NewsFeed";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, "Admin.NewsFeed.Title")
    {
        DescriptionResKey = "Admin.NewsFeed.Title",
        CategoryResKey = "Admin.Dashboard.StoreStatistics",
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
        ],
        Policy = new DashboardWidgetPolicy
        {
            AllowConfigure = false,
            AllowRefresh = false
        }
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;
}
