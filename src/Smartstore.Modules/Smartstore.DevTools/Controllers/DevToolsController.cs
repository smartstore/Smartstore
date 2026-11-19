using Microsoft.AspNetCore.Mvc;
using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using Smartstore.IconKit;
using Smartstore.DevTools.Models;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.DevTools.Controllers;

[Area("Admin")]
//[Route("module/[area]/[action]/{id?}", Name = "Smartstore.DevTools")]
public class DevToolsController : ModuleController
{
    private readonly IIconService _icons;

    public DevToolsController(IIconService icons)
    {
        _icons = icons;
    }

    [AuthorizeAdmin, Permission(DevToolsPermissions.Read)]
    [LoadSetting]
    public async Task<IActionResult> Configure(ProfilerSettings settings)
    {
        var model = MiniMapper.Map<ProfilerSettings, ConfigurationModel>(settings);
        var summary = await _icons.GetCacheSummaryAsync(HttpContext.RequestAborted);

        model.KitFiles = summary.KitFiles
            .Select(file => new IconKitFileModel(file.DisplayName, file.FileName, file.Size))
            .ToArray();
        model.CachedIcons = summary.CachedIcons;

        return View(model);
    }

    [AuthorizeAdmin, Permission(DevToolsPermissions.Update)]
    [HttpPost, SaveSetting]
    public async Task<IActionResult> Configure(ConfigurationModel model, ProfilerSettings settings)
    {
        if (!ModelState.IsValid)
        {
            return await Configure(settings);
        }

        ModelState.Clear();
        MiniMapper.Map(model, settings);

        return RedirectToAction(nameof(Configure));
    }

    [AuthorizeAdmin]
    public IActionResult ProductEditTab(int productId)
    {
        var model = new BackendExtensionModel
        {
            Welcome = "Hello world!"
        };

        ViewData.TemplateInfo.HtmlFieldPrefix = "CustomProperties[DevTools]";
        return View(model);
    }
}