using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.ComponentModel;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Forum.Models;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Forum.Controllers
{
    // TODO: (mg) (core) We need 2 controllers: ForumAdminController & ForumController.    
    [Area("Admin")]
    [Route("[area]/forum/[action]/{id?}")]
    public class ForumAdminController : AdminController
    {
        private readonly ILocalizedEntityService _localizedEntityService;

        public ForumAdminController(ILocalizedEntityService localizedEntityService)
        {
            _localizedEntityService = localizedEntityService;
        }

        [AuthorizeAdmin, Permission(ForumPermissions.Cms.Forum.Read)]
        [LoadSetting]
        public IActionResult Configure(ForumSettings settings, int storeScope)
        {
            var model = MiniMapper.Map<ForumSettings, ConfigurationModel>(settings);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.MetaTitle = settings.GetLocalizedSetting(x => x.MetaTitle, languageId, storeScope, false, false);
                locale.MetaDescription = settings.GetLocalizedSetting(x => x.MetaDescription, languageId, storeScope, false, false);
                locale.MetaKeywords = settings.GetLocalizedSetting(x => x.MetaKeywords, languageId, storeScope, false, false);
            });

            return View(model);
        }

        [AuthorizeAdmin, Permission(ForumPermissions.Cms.Forum.Update)]
        [HttpPost, SaveSetting]
        public async Task<IActionResult> Configure(ConfigurationModel model, ForumSettings settings, int storeScope)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings, storeScope);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedSettingAsync(settings, x => x.MetaTitle, localized.MetaTitle, localized.LanguageId, storeScope);
                await _localizedEntityService.ApplyLocalizedSettingAsync(settings, x => x.MetaDescription, localized.MetaDescription, localized.LanguageId, storeScope);
                await _localizedEntityService.ApplyLocalizedSettingAsync(settings, x => x.MetaKeywords, localized.MetaKeywords, localized.LanguageId, storeScope);
            }

            return RedirectToAction("Configure");
        }

        [Permission(ForumPermissions.Cms.Forum.Read)]
        public IActionResult SearchSettings()
        {
            return Content("TODO: configure search settings....");
        }
    }
}
