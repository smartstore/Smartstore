namespace Smartstore.Core.Localization
{
    public class LocalizationFileResolveResult
    {
        public bool Success { get; set; }
        public string FilePattern { get; set; }
        public string Culture { get; set; }
        public string VirtualPath { get; set; }
    }

    /// <summary>
    /// Responsible for locating a localization file for client scripts
    /// </summary>
    public interface ILocalizationFileResolver
    {
        /// <summary>
        /// Tries to find a matching localization file for a given culture in the following order 
        /// (assuming <paramref name="culture"/> is 'de-DE', <paramref name="virtualPath"/> is '~/scripts/lang-{lang}.js' and <paramref name="fallbackCulture"/> is 'en-US'):
        /// <list type="number">
        ///		<item>Exact match > lang-de-DE.js</item>
        ///		<item>Neutral culture > lang-de.js</item>
        ///		<item>Any region for language > lang-de-CH.js</item>
        ///		<item>Exact match for fallback culture > lang-en-US.js</item>
        ///		<item>Neutral fallback culture > lang-en.js</item>
        ///		<item>Any region for fallback language > lang-en-GB.js</item>
        /// </list>
        /// <para>
        ///     <paramref name="virtualPath"/> must contain the {lang} substitution token in the file name segment for locale code replacement.
        /// </para>
        /// </summary>
        /// <param name="culture">The ISO culture code to get a localization file for, e.g. 'de-DE'</param>
        /// <param name="virtualPath">The virtual path including the {lang} substitution token.</param>
        /// <param name="cache">
        /// Whether caching should be enabled. If <c>false</c>, no attempt is made to read from cache, nor writing the result to the cache.
        /// Cache duration is 24 hours. Automatic eviction on file change is NOT performed.
        /// </param>
        /// <param name="fallbackCulture">Optional.</param>
        /// <returns>Result</returns>
        LocalizationFileResolveResult Resolve(
            string culture,
            string virtualPath,
            bool cache = true,
            string fallbackCulture = "en");
    }
}
