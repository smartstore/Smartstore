namespace Smartstore.Core.Search
{
    public class SearchSort(string name, IndexTypeCode typeCode, bool descending)
    {
        public string FieldName { get; } = name;

        /// <summary>
        /// In this context, <see cref="IndexTypeCode.Empty"/> actually means <c>Score</c>
        /// </summary>
        public IndexTypeCode TypeCode { get; } = typeCode;

        public bool Descending { get; } = descending;

        public override string ToString()
        {
            if (FieldName.IsEmpty())
            {
                return "RELEVANCE";
            }
            else
            {
                return "{0} {1}".FormatInvariant(FieldName, Descending ? "DESC" : "ASC");
            }
        }

        /// <summary>
        /// Sort by relevance (document score). <see cref="FieldName"/> is <c>null</c> in this case.
        /// </summary>
        /// <param name="descending">
        /// <c>true</c> by default: Higher values (scores) are at the front.
        /// </param>
        public static SearchSort ByRelevance(bool descending = true)
        {
            return new SearchSort(null, IndexTypeCode.Empty, descending);
        }

        public static SearchSort ByStringField(string fieldName, bool descending = false)
        {
            return ByField(fieldName, IndexTypeCode.String, descending);
        }

        public static SearchSort ByIntField(string fieldName, bool descending = false)
        {
            return ByField(fieldName, IndexTypeCode.Int32, descending);
        }

        public static SearchSort ByBooleanField(string fieldName, bool descending = false)
        {
            return ByField(fieldName, IndexTypeCode.Boolean, descending);
        }

        public static SearchSort ByDoubleField(string fieldName, bool descending = false)
        {
            return ByField(fieldName, IndexTypeCode.Double, descending);
        }

        public static SearchSort ByDateTimeField(string fieldName, bool descending = false)
        {
            return ByField(fieldName, IndexTypeCode.DateTime, descending);
        }

        private static SearchSort ByField(string fieldName, IndexTypeCode typeCode, bool descending = false)
        {
            Guard.NotEmpty(fieldName);

            return new SearchSort(fieldName, typeCode, descending);
        }
    }
}