using Smartstore.ComponentModel;
using Smartstore.Http;
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

            // TODO: (mg) (core) check URLs. Probably changes.
            model.ApiOdataUrl = WebHelper.GetAbsoluteUrl(Url.Content("~/odata/v1"), Request, true).EnsureEndsWith("/");
            model.ApiOdataMetadataUrl = model.ApiOdataUrl + "$metadata";
            model.SwaggerUrl = WebHelper.GetAbsoluteUrl(Url.Content("~/swagger/ui/index"), Request, true);

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

            await Services.Cache.RemoveByPatternAsync(WebApiService.StatePatternKey);
            NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

            return RedirectToAction(nameof(Configure));
        }
    }
}
