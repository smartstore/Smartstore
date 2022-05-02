namespace Smartstore.Core.Search
{
    public enum SearchMode
    {
        /// <summary>
        /// Term search
        /// </summary>
        ExactMatch = 0,

        /// <summary>
        /// Prefix term search
        /// </summary>
        StartsWith,

        /// <summary>
        /// Wildcard search
        /// </summary>
        Contains
    }

    public enum IndexTypeCode
    {
        Empty = 0,
        Boolean = 3,
        Int32 = 9,
        Double = 14,
        DateTime = 16,
        String = 18
    }
}
