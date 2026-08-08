#nullable enable

using Smartstore.Core.Widgets.Dashboard;

namespace Smartstore.Admin.Components;

/// <summary>
/// Resolves and renders a dashboard by its stable identifier.
/// </summary>
public sealed class DashboardViewComponent : SmartViewComponent
{
    private readonly IDashboardService _dashboardService;

    public DashboardViewComponent(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IViewComponentResult> InvokeAsync(string dashboardId, bool skipUserLayout = false)
    {
        Guard.NotEmpty(dashboardId);

        var model = await _dashboardService.GetDashboardAsync(
            dashboardId,
            skipUserLayout ? null : Services.WorkContext.CurrentCustomer);

        return View(model);
    }
}
