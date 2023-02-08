using Smartstore.Core.Localization;

namespace Smartstore.Core.Search.Indexing
{
    /// <summary>
    /// Represents the reason for what an analyzer is needed.
    /// </summary>
    public enum IndexAnalysisReason
    {
        Search,
        CheckSpelling,
        Highlight
    }

    /// <summary>
    /// Represents the analyzer type for an index field.
    /// </summary>
    public enum IndexAnalyzerType
    {
        /// <summary>
        /// Standard word splitting and filtering.
        /// </summary>
        Standard,

        /// <summary>
        /// Split words only for blanks, no filtering.
        /// </summary>
        Whitespace,

        /// <summary>
        /// No word splitting and no filtering at all.
        /// </summary>
        Keyword,

        /// <summary>
        /// Classic word splitting and filtering.
        /// </summary>
        Classic
    }

    /// <summary>
    /// Represents details of an index analyzer.
    /// </summary>
    public class IndexAnalyzerInfo
    {
        public IndexAnalyzerInfo(string languageCulture, IndexAnalyzerType? type, params string[] fieldNames)
        {
            LanguageCulture = languageCulture;
            AnalyzerType = type;
            FieldNames = fieldNames == null || fieldNames.Length == 0 ? null : fieldNames;
        }

        /// <summary>
        /// Name of the fields which uses this analyzer.
        /// It is recommended to use short names in lower case.
        /// </summary>
        public string[] FieldNames { get; }

        /// <summary>
        /// Language culture if text should be analysed in context of a language. Can be <c>null</c>.
        /// </summary>
        /// <example>en-US</example>
        public string LanguageCulture { get; }

        /// <summary>
        /// Specifies the type of text analysis. Falls back to the MegaSearch default analysis setting if <c>null</c>.
        /// </summary>
        public IndexAnalyzerType? AnalyzerType { get; }
    }


    /// <summary>
    /// Represents an index analyzer for an index scope.
    /// </summary>
    public interface IIndexAnalyzer
    {
        /// <summary>
        /// Gets the default analyzer. It is used when no specific analyzer has been set for an index field.
        /// Falls back to the first analyzer defined by <see cref="IIndexStore"/>.
        /// </summary>
        /// <param name="reason">Reason for requesting the analyzer.</param>
        /// <param name="indexStore">Related <see cref="IIndexStore"/> instance.</param>
        /// <returns>The default analyzer</returns>
        IndexAnalyzerType? GetDefaultAnalyzerType(IndexAnalysisReason reason, IIndexStore indexStore);

        /// <summary>
        /// Gets a list of text analyzer to be used. An analyzer specifies how the content of an index field
        /// is to be processed during indexing and searching.
        /// </summary>
        /// <param name="reason">Reason for requesting the analyzer.</param>
        /// <param name="languages">List of all active languages. The first represents the default language of the store.</param>
        /// <param name="indexStore">Related <see cref="IIndexStore"/> instance.</param>
        /// <returns>List of analyzer.</returns>
        IList<IndexAnalyzerInfo> GetAnalyzerInfos(IndexAnalysisReason reason, IList<Language> languages, IIndexStore indexStore);
    }
}
