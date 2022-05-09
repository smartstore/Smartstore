using Smartstore.Core.Localization;

namespace Smartstore.Core.Search.Indexing
{
    public enum FieldAnalysisReason
    {
        Search,
        CheckSpelling,
        Highlight
    }

    public enum FieldAnalysisType
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

    public class FieldAnalysisInfo
    {
        public string[] FieldNames { get; init; }
        public Language Language { get; init; }
        public FieldAnalysisType? AnalysisType { get; init; }
    }
}
