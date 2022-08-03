using Smartstore.Admin.Models.Localization;
using Smartstore.ComponentModel;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Components
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

            var mapper = MapperFactory.GetMapper<Language, LanguageModel>();
            ViewBag.AvailableLanguages = await _languageService
                 .GetAllLanguages(false, Services.StoreContext.CurrentStore.Id)
                 .SelectAwait(async x => await mapper.MapAsync(x))
                 .AsyncToList();

            return View();
        }
    }
}
