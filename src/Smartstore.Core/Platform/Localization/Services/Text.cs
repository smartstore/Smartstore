namespace Smartstore.Core.Localization
{
    public partial class Text : IText
    {
        private readonly ILocalizationService _localizationService;

        public Text(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        public virtual LocalizedString Get(string key, params object[] args)
        {
            // INFO: (perf) Hot path code, don't call GetEx().
            var value = _localizationService.GetResource(key, 0, returnEmptyIfNotFound: true);

            if (string.IsNullOrEmpty(value))
            {
                return new LocalizedString(key, true);
            }

            if (args?.Length == 0)
            {
                return new LocalizedString(value);
            }

            return new LocalizedString(value, key, args);
        }

        public virtual LocalizedString GetEx(string key, int languageId, params object[] args)
        {
            var value = _localizationService.GetResource(key, languageId);

            if (string.IsNullOrEmpty(value))
            {
                return new LocalizedString(key);
            }

            if (args?.Length == 0)
            {
                return new LocalizedString(value);
            }

            return new LocalizedString(value, key, args);
        }
    }
}
