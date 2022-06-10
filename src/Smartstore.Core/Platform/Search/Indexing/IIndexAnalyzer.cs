using Smartstore.Core.Localization;

namespace Smartstore.Core.Search.Indexing
{
    public enum IndexAnalysisReason
    {
        Search,
        CheckSpelling,
        Highlight
    }

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

    public class IndexAnalyzerInfo
    {
        public IndexAnalyzerInfo(string languageCulture, IndexAnalyzerType? type, params string[] fieldNames)
        {
            LanguageCulture = languageCulture;
            AnalyzerType = type;
            FieldNames = fieldNames == null || fieldNames.Length == 0 ? null : fieldNames;
        }

        public string[] FieldNames { get; }
        public string LanguageCulture { get; }
        public IndexAnalyzerType? AnalyzerType { get; }
    }


    public interface IIndexAnalyzer
    {
        IndexAnalyzerType? GetDefaultAnalyzerType(IndexAnalysisReason reason, IIndexStore indexStore);

        IList<IndexAnalyzerInfo> GetAnalyzerInfos(IndexAnalysisReason reason, IList<Language> languages, IIndexStore indexStore);
    }
}
