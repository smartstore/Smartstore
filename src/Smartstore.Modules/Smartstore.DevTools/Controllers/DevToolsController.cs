using Microsoft.AspNetCore.Mvc;
using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using Smartstore.DevTools.Models;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.DevTools.Controllers
{
    [Area("Admin")]
    //[Route("module/[area]/[action]/{id?}", Name = "Smartstore.DevTools")]
    public class DevToolsController : ModuleController
    {
        [AuthorizeAdmin, Permission(DevToolsPermissions.Read)]
        [LoadSetting]
        public IActionResult Configure(ProfilerSettings settings)
        {
            var model = MiniMapper.Map<ProfilerSettings, ConfigurationModel>(settings);
            return View(model);
        }

        [AuthorizeAdmin, Permission(DevToolsPermissions.Update)]
        [HttpPost, SaveSetting]
        public IActionResult Configure(ConfigurationModel model, ProfilerSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
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
}