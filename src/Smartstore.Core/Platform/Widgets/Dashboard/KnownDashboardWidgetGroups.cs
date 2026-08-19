#nullable enable

using Smartstore.Core.Localization;

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Provides commonly used groups for organizing widgets in dashboard selection interfaces.
/// </summary>
public static class KnownDashboardWidgetGroups
{
    public static ResolvableText Sales { get; } = ResolvableText.Resource("Admin.Sales");
    public static ResolvableText Customers { get; } = ResolvableText.Resource("Admin.Customers");
    public static ResolvableText Analytics { get; } = ResolvableText.Resource("Admin.Plugins.KnownGroup.Analytics");
    public static ResolvableText Catalog { get; } = ResolvableText.Resource("Admin.Catalog");
    public static ResolvableText Marketing { get; } = ResolvableText.Resource("Admin.Plugins.KnownGroup.Marketing");
    public static ResolvableText Content { get; } = ResolvableText.Resource("Admin.Plugins.KnownGroup.CMS");
    public static ResolvableText System { get; } = ResolvableText.Resource("Admin.System");
}
