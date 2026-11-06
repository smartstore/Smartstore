#nullable enable

using System.Text.Json.Serialization;

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Identifies responsive layout tiers in ascending order, independently of their CSS pixel widths.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DashboardBreakpoint>))]
public enum DashboardBreakpoint
{
    [JsonStringEnumMemberName("xs")]
    Xs,

    [JsonStringEnumMemberName("sm")]
    Sm,

    [JsonStringEnumMemberName("md")]
    Md,

    [JsonStringEnumMemberName("lg")]
    Lg,

    [JsonStringEnumMemberName("xl")]
    Xl,

    [JsonStringEnumMemberName("xxl")]
    Xxl
}
