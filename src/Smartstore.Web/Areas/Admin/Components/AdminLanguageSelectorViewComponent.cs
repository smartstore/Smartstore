using Microsoft.AspNetCore.Mvc;
using Smartstore.Admin.Models.Localization;
using Smartstore.ComponentModel;
using Smartstore.Core.Localization;
using Smartstore.Web.Components;
using System.Threading.Tasks;

namespace Smartstore.Web.Areas.Admin.Components
{
    public class AdminLanguageSelectorViewComponent : SmartViewComponent
    {
        private readonly ILanguageService _languageService;

        public AdminLanguageSelectorViewComponent(ILanguageService languageService)
        {
            _languageService = languageService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            ViewBag.CurrentLanguage = await MapperFactory.MapAsync<Language, LanguageModel>(Services.WorkContext.WorkingLanguage);
            ViewBag.AvailableLanguages = await _languageService
                 .GetAllLanguages(storeId: Services.StoreContext.CurrentStore.Id)
                 .SelectAsync(async x => await MapperFactory.MapAsync<Language, LanguageModel>(x))
                 .AsyncToList();
            
            return View();
        }
    }
}
