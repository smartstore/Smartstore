using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Dashboard;

/// <summary>
/// Contains the editable item limit of a dashboard widget.
/// </summary>
[LocalizedDisplay("Admin.Dashboard.Widget.")]
public class DashboardWidgetCountConfigurationModel
{
    /// <summary>
    /// Gets or sets the maximum number of items to display.
    /// </summary>
    [LocalizedDisplay("*Count")]
    public int? Count { get; set; }

    /// <summary>
    /// Gets or sets the item count used when no override is configured.
    /// </summary>
    public int DefaultCount { get; set; } = 7;

}
