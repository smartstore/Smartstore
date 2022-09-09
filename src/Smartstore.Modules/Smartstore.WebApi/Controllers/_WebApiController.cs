using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;
using Smartstore.WebApi.Models;

namespace Smartstore.WebApi.Controllers
{
    public class WebApiController : AdminController
    {
        [Permission(WebApiPermissions.Read)]
        [LoadSetting]
        public IActionResult Configure(WebApiSettings settings)
        {
            var model = MiniMapper.Map<WebApiSettings, ConfigurationModel>(settings);

            return View(model);
        }

        [Permission(WebApiPermissions.Update)]
        [HttpPost, SaveSetting]
        public async Task<IActionResult> Configure(ConfigurationModel model, WebApiSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
            }

            MiniMapper.Map(model, settings);
            await Services.Cache.RemoveAsync(WebApiService.StateKey);

            NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

            return RedirectToAction(nameof(Configure));
        }
    }
}
