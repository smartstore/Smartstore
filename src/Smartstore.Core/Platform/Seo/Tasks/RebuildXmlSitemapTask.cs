using Smartstore.Core.Configuration;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Scheduling;

namespace Smartstore.Core.Seo
{
    public class RebuildXmlSitemapTask : ITask
    {
        private readonly IStoreContext _storeContext;
        private readonly ILanguageService _languageService;
        private readonly IXmlSitemapGenerator _generator;
        private readonly ISettingFactory _settingFactory;

        public RebuildXmlSitemapTask(
            IStoreContext storeContext,
            ILanguageService languageService,
            IXmlSitemapGenerator generator,
            ISettingFactory settingFactory)
        {
            _storeContext = storeContext;
            _languageService = languageService;
            _generator = generator;
            _settingFactory = settingFactory;
        }

        public async Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
        {
            var stores = _storeContext.GetAllStores();

            foreach (var store in stores)
            {
                var languages = _languageService.GetAllLanguages(false, store.Id);
                var buildContext = new XmlSitemapBuildContext(store, languages.ToArray(), _settingFactory, _storeContext.IsSingleStoreMode())
                {
                    CancellationToken = cancelToken,
                    ProgressCallback = OnProgress
                };

                await _generator.RebuildAsync(buildContext);
            }

            Task OnProgress(int value, int max, string msg)
            {
                return ctx.SetProgressAsync(value, max, msg);
            }
        }
    }
}