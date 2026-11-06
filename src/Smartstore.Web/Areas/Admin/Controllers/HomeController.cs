using Smartstore.Admin.Infrastructure.Dashboard;
using Smartstore.Admin.Models.Dashboard;
using Smartstore.Core.Security;
using Smartstore.Core.Web;
using Smartstore.Core.Widgets.Dashboard;
using System.Text.Json;

namespace Smartstore.Admin.Controllers;

public class HomeController : AdminController
{
    private readonly IUserAgentFactory _userAgentFactory;
    private readonly IUserAgent _userAgent;
    private readonly IDashboardService _dashboardService;

    public HomeController(IUserAgentFactory userAgentFactory, IUserAgent userAgent,
        IDashboardService dashboardService)
    {
        _userAgentFactory = userAgentFactory;
        _userAgent = userAgent;
        _dashboardService = dashboardService;
    }

    #region Dashboard

    /// <summary>Renders the configuration surface for an authorized widget in the current draft.</summary>
    [HttpPost, ValidateAntiForgeryToken, Permission(Permissions.Dashboard.Read), RequestSizeLimit(262144)]
    public async Task<IActionResult> DashboardConfigure(
        string instanceId,
        string widgetSystemName,
        DashboardLayoutLayer layer,
        string changesJson,
        bool reset = false)
    {
        if (!ModelState.IsValid || !instanceId.HasValue() || !widgetSystemName.HasValue())
        {
            return BadRequest();
        }

        await AuthorizeDashboardEditAsync(layer);

        try
        {
            var state = await ReadDashboardDraftAsync(changesJson, layer, reset);
            var customer = Services.WorkContext.CurrentCustomer;
            if (!reset)
            {
                await _dashboardService.ValidateDraftAsync(state.Layout, customer, layer, HttpContext.RequestAborted);
            }

            var instance = state.Layout.Widgets.FirstOrDefault(x => x.Id == instanceId);
            if (instance == null || instance.WidgetSystemName != widgetSystemName)
            {
                return NotFound();
            }

            var widget = _dashboardService.GetWidget(widgetSystemName);
            var context = new DashboardWidgetContext { DashboardId = state.Layout.Id, Customer = customer, IsEditMode = true };
            var policy = DashboardWidgetPolicy.Combine(widget.Descriptor.Policy, instance.Policy);
            if (!policy.AllowConfigure || !await widget.IsAvailableAsync(context, HttpContext.RequestAborted))
            {
                return Forbid();
            }

            await widget.ValidateSettingsAsync(instance.Settings, HttpContext.RequestAborted);
            var configuration = widget.CreateConfigurationWidget(context, instance);
            if (configuration == null)
            {
                return NotFound();
            }

            return PartialView("_DashboardConfiguration", configuration);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            Logger.Warn(ex, "Invalid dashboard widget configuration.");
            return BadRequest();
        }
    }


    [Permission(Permissions.Dashboard.Read)]
    public async Task<IActionResult> Index(bool edit = false, DashboardLayoutLayer? layer = null)
    {
        return View(await BuildDashboardModelAsync(layer, edit));
    }

    /// <summary>
    /// Reloads one widget from the selected dashboard or a validated, unpersisted draft.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken, Permission(Permissions.Dashboard.Read), RequestSizeLimit(262144)]
    public async Task<IActionResult> DashboardWidget(
        string instanceId,
        bool refresh = false,
        Dictionary<string, string> parameters = null,
        string changesJson = null,
        DashboardLayoutLayer? layer = null,
        bool reset = false)
    {
        if (!ModelState.IsValid || !instanceId.HasValue())
        {
            return BadRequest();
        }

        var customer = Services.WorkContext.CurrentCustomer;
        if (changesJson != null)
        {
            if (!layer.HasValue)
            {
                return BadRequest();
            }

            await AuthorizeDashboardEditAsync(layer.Value);
        }

        try
        {
            DashboardLayoutState state;
            if (changesJson != null)
            {
                state = await ReadDashboardDraftAsync(changesJson, layer.Value, reset);
                await _dashboardService.ValidateDraftAsync(state.Layout, customer, layer.Value, HttpContext.RequestAborted);
            }
            else
            {
                state = await _dashboardService.GetLayoutStateAsync(
                    AdminDashboardLayoutProvider.Id,
                    customer,
                    await GetDashboardViewLayerAsync(),
                    cancelToken: HttpContext.RequestAborted);
            }

            var item = await _dashboardService.PrepareWidgetAsync(
                state.Layout,
                instanceId,
                customer,
                refresh,
                parameters,
                HttpContext.RequestAborted);

            if (item == null)
            {
                return NotFound();
            }

            return PartialView("_DashboardWidget", item);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            return BadRequest();
        }
    }

    private async Task<DashboardLayoutLayer> GetDashboardViewLayerAsync()
    {
        var selected = await Services.Permissions.AuthorizeAsync(Permissions.Dashboard.ManageGlobal)
            ? _dashboardService.GetSelectedLayer(AdminDashboardLayoutProvider.Id, Services.WorkContext.CurrentCustomer)
            : DashboardLayoutLayer.User;

        return selected == DashboardLayoutLayer.Global ? selected : DashboardLayoutLayer.User;
    }

    /// <summary>
    /// Changes only the current user's dashboard view selection.
    /// </summary>
    [HttpPost, Permission(Permissions.Dashboard.Read)]
    public async Task<IActionResult> DashboardSelect(DashboardLayoutLayer layer)
    {
        if (!ModelState.IsValid || layer == DashboardLayoutLayer.System || !Enum.IsDefined(layer))
        {
            return BadRequest();
        }
        if (!await Services.Permissions.AuthorizeAsync(Permissions.Dashboard.ManageGlobal))
        {
            throw new AccessDeniedException();
        }
        await _dashboardService.SelectLayerAsync(AdminDashboardLayoutProvider.Id, Services.WorkContext.CurrentCustomer, layer, HttpContext.RequestAborted);
        return RedirectToAction(nameof(Index), new { area = "Admin" });
    }

    /// <summary>
    /// Renders a draft, optionally adding a widget or previewing a reset; never persists it.
    /// </summary>
    [HttpPost, Permission(Permissions.Dashboard.Read), RequestSizeLimit(262144)]
    public async Task<IActionResult> DashboardPreview(DashboardLayoutLayer layer, string changesJson, bool reset, string command, string widgetSystemName, DashboardLayoutLayer? templateSource = null)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }
        await AuthorizeDashboardEditAsync(layer);
        try
        {
            reset = reset || command == "reset";
            if (reset)
            {
                templateSource = null;
            }
            var state = await ReadDashboardDraftAsync(changesJson, layer, reset);
            var draft = state.Layout;
            if (!reset)
            {
                await _dashboardService.ValidateDraftAsync(draft, Services.WorkContext.CurrentCustomer, layer, HttpContext.RequestAborted);
            }
            if (command == "add")
            {
                var widget = _dashboardService.GetWidget(widgetSystemName);
                var context = new DashboardWidgetContext { DashboardId = draft.Id, Customer = Services.WorkContext.CurrentCustomer, IsEditMode = true };
                if (!await widget.IsAvailableAsync(context, HttpContext.RequestAborted))
                {
                    return Forbid();
                }
                if (!widget.Descriptor.AllowMultipleInstances && draft.Widgets.Any(x => x.WidgetSystemName == widgetSystemName))
                {
                    throw new InvalidOperationException("A singleton widget already exists.");
                }
                var order = checked(draft.Widgets.Select(x => x.Order).DefaultIfEmpty(0).Max() + 100);
                var instance = DashboardEditorMapper.CreateWidgetInstance(widget, "widget-" + Guid.NewGuid().ToString("N"), order);
                draft.Widgets.Add(instance with { Positions = DashboardEditorMapper.GetInsertionPositions(draft, widget.Descriptor.DefaultSize) });
                reset = false;
            }
            return View("Index", await BuildDashboardModelAsync(layer, true, state, reset, true, templateSource));
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            Logger.Warn(ex, "Invalid dashboard preview.");
            NotifyError(T("Admin.Dashboard.Editor.Invalid"));
            return View("Index", await BuildDashboardModelAsync(layer, true));
        }
    }

    /// <summary>
    /// Commits or resets exactly one authorized layer, retaining the draft on write errors.
    /// </summary>
    [HttpPost, Permission(Permissions.Dashboard.Read), RequestSizeLimit(262144)]
    public async Task<IActionResult> DashboardSave(DashboardLayoutLayer layer, string changesJson, bool reset, string next = null, DashboardLayoutLayer? templateSource = null, DashboardLayoutLayer? saveLayer = null)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }
        await AuthorizeDashboardEditAsync(layer);
        var target = saveLayer ?? layer;
        if (target != layer)
        {
            await AuthorizeDashboardEditAsync(target);
        }
        DashboardLayoutState state;
        try
        {
            state = await ReadDashboardDraftAsync(changesJson, layer, reset);
            if (target != layer)
            {
                if (!reset)
                {
                    await _dashboardService.ValidateDraftAsync(state.Layout, Services.WorkContext.CurrentCustomer, layer, HttpContext.RequestAborted);
                }
                var targetState = await _dashboardService.GetLayoutStateAsync(AdminDashboardLayoutProvider.Id,
                    Services.WorkContext.CurrentCustomer, target, cancelToken: HttpContext.RequestAborted);
                var catalog = await GetAvailableDashboardWidgetsAsync();
                DashboardEditorMapper.ApplyTemplate(state.Layout, targetState.Layout, catalog, _dashboardService);
                state = targetState;
                templateSource = layer;
                layer = target;
                reset = false;
            }
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            Logger.Warn(ex, "Invalid dashboard draft.");
            NotifyError(T("Admin.Dashboard.Editor.Invalid"));
            return View("Index", await BuildDashboardModelAsync(layer, true));
        }
        try
        {
            await _dashboardService.SaveLayoutAsync(state.Layout, Services.WorkContext.CurrentCustomer, layer, reset, HttpContext.RequestAborted);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or JsonException)
        {
            Logger.Warn(ex, "Rejected dashboard customization.");
            NotifyError(T("Admin.Dashboard.Editor.Invalid"));
            return View("Index", await BuildDashboardModelAsync(layer, true, state, reset, true, templateSource));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Smartstore.IO.FileSystemException or Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            Logger.Error(ex, "Failed to save dashboard layout.");
            NotifyError(T("Admin.Dashboard.Editor.SaveFailed"));
            return View("Index", await BuildDashboardModelAsync(layer, true, state, reset, true, templateSource));
        }
        NotifySuccess(T("Admin.Dashboard.Editor.Saved"));
        var transition = next?.Split(':');
        DashboardLayoutLayer? editLayer = null;
        if (transition?.Length == 2 && transition[0] == "edit" &&
            Enum.TryParse<DashboardLayoutLayer>(transition[1], out var nextLayer) &&
            Enum.IsDefined(nextLayer) && nextLayer != DashboardLayoutLayer.System)
        {
            editLayer = nextLayer;
        }

        if (next != "cancel:" && await Services.Permissions.AuthorizeAsync(Permissions.Dashboard.ManageGlobal))
        {
            try
            {
                await _dashboardService.SelectLayerAsync(AdminDashboardLayoutProvider.Id, Services.WorkContext.CurrentCustomer, layer, HttpContext.RequestAborted);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                Logger.Error(ex, "Dashboard layout was saved, but the view selection could not be saved.");
                NotifyWarning(T("Admin.Dashboard.Editor.SelectionFailed"));
            }
        }

        return editLayer.HasValue
            ? RedirectToAction(nameof(Index), new { area = "Admin", edit = true, layer = editLayer.Value })
            : RedirectToAction(nameof(Index), new { area = "Admin" });
    }

    /// <summary>
    /// Checks the independently granted write permission for a mutable layer.
    /// </summary>
    private async Task AuthorizeDashboardEditAsync(DashboardLayoutLayer layer)
    {
        if (layer == DashboardLayoutLayer.System || !Enum.IsDefined(layer) ||
            !await Services.Permissions.AuthorizeAsync(layer == DashboardLayoutLayer.User ? Permissions.Dashboard.Personalize : Permissions.Dashboard.ManageGlobal))
            throw new AccessDeniedException();
    }

    /// <summary>
    /// Reconstructs a draft from explicit client changes and trusted server metadata.
    /// </summary>
    private async Task<DashboardLayoutState> ReadDashboardDraftAsync(string json, DashboardLayoutLayer layer, bool reset)
    {
        var state = await _dashboardService.GetLayoutStateAsync(AdminDashboardLayoutProvider.Id,
            Services.WorkContext.CurrentCustomer, layer, reset, HttpContext.RequestAborted);
        if (!reset)
        {
            var changes = DashboardEditorMapper.ReadChanges(json);
            DashboardEditorMapper.ApplyChanges(changes, state.Layout, _dashboardService);
        }
        return state;
    }

    /// <summary>
    /// Builds a normal view or an isolated, unpersisted edit session.
    /// </summary>
    private async Task<DashboardEditorModel> BuildDashboardModelAsync(DashboardLayoutLayer? layer, bool editing,
        DashboardLayoutState state = null, bool reset = false, bool dirty = false, DashboardLayoutLayer? templateSource = null)
    {
        var customer = Services.WorkContext.CurrentCustomer;
        var canPersonalize = await Services.Permissions.AuthorizeAsync(Permissions.Dashboard.Personalize);
        var canManageGlobal = await Services.Permissions.AuthorizeAsync(Permissions.Dashboard.ManageGlobal);
        var selected = await GetDashboardViewLayerAsync();
        var target = editing ? layer ?? selected : selected;
        if (editing && layer == null && target == DashboardLayoutLayer.User && !canPersonalize && canManageGlobal)
        {
            target = DashboardLayoutLayer.Global;
        }
        if (editing)
        {
            await AuthorizeDashboardEditAsync(target);
        }
        var catalog = editing ? await GetAvailableDashboardWidgetsAsync() : [];
        if (state == null)
        {
            state = await _dashboardService.GetLayoutStateAsync(AdminDashboardLayoutProvider.Id, customer, target, reset, HttpContext.RequestAborted);
        }
        else if (!reset)
        {
            _dashboardService.ApplyDraftMetadata(state.Layout, state, customer);
        }
        return new DashboardEditorModel
        {
            State = state,
            Dashboard = await _dashboardService.PrepareDashboardAsync(state.Layout, customer, editing, HttpContext.RequestAborted),
            Catalog = catalog,
            CanPersonalize = canPersonalize,
            CanManageGlobal = canManageGlobal,
            IsEditing = editing,
            Reset = reset,
            IsDirty = dirty,
            TemplateSource = !reset && templateSource.HasValue && Enum.IsDefined(templateSource.Value) ? templateSource : null
        };
    }

    private async Task<List<DashboardWidgetDescriptor>> GetAvailableDashboardWidgetsAsync()
    {
        var catalog = new List<DashboardWidgetDescriptor>();
        var context = new DashboardWidgetContext
        {
            DashboardId = AdminDashboardLayoutProvider.Id,
            Customer = Services.WorkContext.CurrentCustomer,
            IsEditMode = true
        };
        foreach (var descriptor in _dashboardService.GetDescriptors())
        {
            if (await _dashboardService.GetWidget(descriptor.SystemName).IsAvailableAsync(context, HttpContext.RequestAborted))
            {
                catalog.Add(descriptor);
            }
        }
        return catalog;
    }

    #endregion

    public IActionResult About()
    {
        return View();
    }

    public IActionResult UaTester(string ua = null)
    {
        if (ua.HasValue())
        {
            return View(_userAgentFactory.CreateUserAgent(ua, false));
        }
        else
        {
            return View(_userAgent);
        }
    }
}
