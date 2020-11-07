using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Language service interface
    /// </summary>
    public interface ILanguageService
    {
        /// <summary>
        /// Gets all (cached) languages.
        /// </summary>
        /// <param name="includeHidden">A value indicating whether to include hidden records</param>
        /// <param name="storeId">Load records only in specified store; pass 0 to load all records.</param>
        /// <returns>Language collection</returns>
        Task<List<Language>> GetAllLanguagesAsync(bool includeHidden = false, int storeId = 0);

        /// <summary>
        /// Determines whether a language is active/published
        /// </summary>
        /// <param name="languageId">The id of the language to check</param>
        /// <param name="storeId">The store id</param>
        /// <returns><c>true</c> when the language is published, <c>false</c> otherwise</returns>
        Task<bool> IsPublishedLanguageAsync(int languageId, int storeId = 0);

        /// <summary>
        /// Determines whether a language is active/published
        /// </summary>
        /// <param name="seoCode">The SEO code of the language to check</param>
        /// <param name="storeId">The store id</param>
        /// <returns><c>true</c> when the language is published, <c>false</c> otherwise</returns>
        Task<bool> IsPublishedLanguageAsync(string seoCode, int storeId = 0);

        /// <summary>
        /// Gets the seo code of the default (first) active language
        /// </summary>
        /// <param name="storeId">The store id</param>
        /// <returns>The seo code</returns>
        Task<string> GetDefaultLanguageSeoCodeAsync(int storeId = 0);

        /// <summary>
        /// Gets the id of the default (first) active language
        /// </summary>
        /// <param name="storeId">The store id</param>
        /// <returns>The language id</returns>
        Task<int> GetDefaultLanguageIdAsync(int storeId = 0);
    }
}