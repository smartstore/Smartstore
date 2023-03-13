using System.Globalization;

namespace Smartstore.Core.Localization
{
    public static class ILanguageExtensions
    {
        public static string GetTwoLetterISOLanguageName(this ILanguage language)
        {
            Guard.NotNull(language);
            
            if (language.UniqueSeoCode.HasValue())
            {
                return language.UniqueSeoCode;
            }

            try
            {
                return new CultureInfo(language.LanguageCulture).TwoLetterISOLanguageName;
            }
            catch
            {
                return null;
            }
        }
    }
}
