using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Configuration;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Scheduling;

namespace Smartstore.Core.Seo
{
    public class XmlSitemapBuildContext
    {
        private readonly ISettingFactory _settingFactory;
        private readonly bool _isSingleStoreMode;

        public XmlSitemapBuildContext(Store store, Language[] languages, ISettingFactory settingFactory, bool isSingleStoreMode)
        {
            Guard.NotNull(store);
            Guard.NotEmpty(languages);

            Store = store;
            Languages = languages;

            _settingFactory = settingFactory;
            _isSingleStoreMode = isSingleStoreMode;

            RequestStoreId = _isSingleStoreMode ? 0 : store.Id;
        }

        public CancellationToken CancellationToken { get; init; }
        public ProgressCallback ProgressCallback { get; set; }
        public Store Store { get; init; }
        public int RequestStoreId { get; init; }
        public Language[] Languages { get; init; }
        public int MaximumNodeCount { get; init; } = XmlSitemapGenerator.MaximumSiteMapNodeCount;

        public T LoadSettings<T>() where T : ISettings, new()
        {
            return _settingFactory.LoadSettings<T>(RequestStoreId);
        }

        public Task<T> LoadSettingsAsync<T>() where T : ISettings, new()
        {
            return _settingFactory.LoadSettingsAsync<T>(RequestStoreId);
        }
    }

    public class XmlSitemapBuildNodeContext
    {
        public LinkGenerator LinkGenerator { get; internal set; }
        public int DefaultLanguageId { get; internal set; }

        /// <summary>
        /// Gets a list of languages for which alternative links are to be created.
        /// </summary>
        public ICollection<LinkLanguage> LinkLanguages { get; internal set; }

        public record LinkLanguage
        {
            public Language Language { get; init; }
            public string BaseUrl { get; init; }
        }
    }
}
