namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Language service interface
    /// </summary>
    public partial interface ILanguageService
    {
        /// <summary>
        /// Determines whether the given (or current) store has multiple active languages.
        /// </summary>
        /// <param name="storeId">The store id</param>
        bool IsMultiLanguageEnvironment(int storeId = 0);

        /// <summary>
        /// Gets all (cached) languages.
        /// </summary>
        /// <param name="includeHidden">A value indicating whether to include hidden records</param>
        /// <param name="storeId">Load records only in specified store; pass 0 to load all records.</param>
        /// <param name="tracked">Whether to put entities to EF change tracker.</param>
        /// <returns>Language collection</returns>
        List<Language> GetAllLanguages(bool includeHidden = false, int storeId = 0, bool tracked = false);

        /// <summary>
        /// Gets all (cached) languages.
        /// </summary>
        /// <param name="includeHidden">A value indicating whether to include hidden records</param>
        /// <param name="storeId">Load records only in specified store; pass 0 to load all records.</param>
        /// <param name="tracked">Whether to put entities to EF change tracker.</param>
        /// <returns>Language collection</returns>
        Task<List<Language>> GetAllLanguagesAsync(bool includeHidden = false, int storeId = 0, bool tracked = false);

        /// <summary>
        /// Determines whether a language is active/published
        /// </summary>
        /// <param name="languageId">The id of the language to check</param>
        /// <param name="storeId">The store id</param>
        /// <returns><c>true</c> when the language is published, <c>false</c> otherwise</returns>
        bool IsPublishedLanguage(int languageId, int storeId = 0);

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
        bool IsPublishedLanguage(string seoCode, int storeId = 0);

        /// <summary>
        /// Determines whether a language is active/published
        /// </summary>
        /// <param name="seoCode">The SEO code of the language to check</param>
        /// <param name="storeId">The store id</param>
        /// <returns><c>true</c> when the language is published, <c>false</c> otherwise</returns>
        Task<bool> IsPublishedLanguageAsync(string seoCode, int storeId = 0);

        /// <summary>
        /// Gets the seo code of the master (first) active language
        /// </summary>
        /// <param name="storeId">The store id</param>
        /// <returns>The seo code</returns>
        string GetMasterLanguageSeoCode(int storeId = 0);

        /// <summary>
        /// Gets the seo code of the master (first) active language
        /// </summary>
        /// <param name="storeId">The store id</param>
        /// <returns>The seo code</returns>
        Task<string> GetMasterLanguageSeoCodeAsync(int storeId = 0);

        /// <summary>
        /// Gets the id of the master (first) active language
        /// </summary>
        /// <param name="storeId">The store id</param>
        /// <returns>The language id</returns>
        int GetMasterLanguageId(int storeId = 0);

        /// <summary>
        /// Gets the id of the master (first) active language
        /// </summary>
        /// <param name="storeId">The store id</param>
        /// <returns>The language id</returns>
        Task<int> GetMasterLanguageIdAsync(int storeId = 0);
    }
}