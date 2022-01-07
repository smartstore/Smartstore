using System.Globalization;
using FluentValidation;
using FluentValidation.Resources;

namespace Smartstore.Web.Modelling.Validation
{
    internal class ValidatorLanguageManager : LanguageManager
    {
        private readonly bool _canResolveServices;

        public ValidatorLanguageManager(IApplicationContext appContext)
        {
            _canResolveServices = appContext.IsInstalled && appContext.IsWebHost;
        }

        public override string GetString(string key, CultureInfo culture = null)
        {
            string result = base.GetString(key, culture);

            if (_canResolveServices)
            {
                // (Perf) although FV expects a culture parameter, we gonna ignore it.
                // It's highly unlikely that it is anything different than our WorkingLanguage.
                var services = EngineContext.Current.ResolveService<ICommonServices>();
                result = services.Localization.GetResource("Validation." + key, logIfNotFound: false, defaultValue: result, returnEmptyIfNotFound: true);
            }

            return result;
        }

        public string GetErrorMessage(string key, string propertyName)
        {
            Guard.NotEmpty(key, nameof(key));
            Guard.NotNull(propertyName, nameof(propertyName));

            var template = GetString(key);
            var formatter = ValidatorOptions.Global.MessageFormatterFactory()
                .AppendPropertyName(propertyName);

            return formatter.BuildMessage(template);
        }
    }
}
