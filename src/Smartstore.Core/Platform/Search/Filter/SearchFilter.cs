using Parlot.Fluent;
using static Smartstore.Core.Content.Media.Icons.IconDescription;

namespace Smartstore.Core.Search
{
    public class SearchFilter : SearchFilterBase, IAttributeSearchFilter
    {
        protected SearchFilter()
            : base()
        {
        }

        public IndexTypeCode TypeCode
        {
            get;
            protected set;
        }

        public object Term
        {
            get;
            internal set;
        }

        /// <summary>
        /// Specifies the search mode.
        /// Note that the mode has an impact on the performance of the search. <see cref="SearchMode.ExactMatch"/> is the fastest,
        /// <see cref="SearchMode.StartsWith"/> is slower and <see cref="SearchMode.Contains"/> the slowest.
        /// </summary>
        public SearchMode Mode 
        { 
            get; 
            protected set; 
        } = SearchMode.Contains;

        /// <summary>
        /// A value indicating whether to escape the search term.
        /// </summary>
        public bool Escape
        {
            get;
            protected set;
        }

        public bool IsNotAnalyzed
        {
            get;
            protected set;
        }

        public int ParentId
        {
            get;
            protected set;
        }

        #region Fluent builder

        /// <summary>
        /// Mark a clause as a mandatory match. By default all clauses are optional.
        /// </summary>
        /// <param name="mandatory">Whether the clause is mandatory or not.</param>
        public SearchFilter Mandatory(bool mandatory = true)
        {
            Occurence = mandatory ? SearchFilterOccurence.Must : SearchFilterOccurence.MustNot;
            return this;
        }

        /// <summary>
        /// Mark a clause as a forbidden match (MustNot).
        /// </summary>
        public SearchFilter Forbidden()
        {
            Occurence = SearchFilterOccurence.MustNot;
            return this;
        }

        /// <summary>
        /// Specifies whether the clause should be matched exactly, like 'app' won't match 'apple' (applied on string clauses only).
        /// </summary>
        public SearchFilter ExactMatch()
        {
            Mode = SearchMode.ExactMatch;
            return this;
        }

        public SearchFilter StartsWith()
        {
            Mode = SearchMode.StartsWith;
            return this;
        }

        /// <summary>
        /// Specifies that the searched value will not be tokenized (applied on string clauses only)
        /// </summary>
        public SearchFilter NotAnalyzed()
        {
            IsNotAnalyzed = true;
            return this;
        }

        /// <summary>
        /// Applies a parent identifier.
        /// </summary>
        /// <param name="parentId">Parent identifier</param>
        public SearchFilter HasParent(int parentId)
        {
            ParentId = parentId;
            return this;
        }

        /// <summary>
        /// Applies a specific boost factor.
        /// </summary>
        /// <param name="weight">
        /// The boost factor. The higher the boost factor, the more relevant the search term will be
        /// and the more in front the search hit will be ranked/scored.
        /// <c>null</c> to apply the standard boost factor.
        /// </param>
        /// <remarks>
        /// The default boost factor depends on the search engine.
        /// The default MegaSearch/Lucene search time boost factor is 1.0.
        /// </remarks>
        public SearchFilter Weighted(float? weight)
        {
            Boost = weight;
            return this;
        }

        #endregion

        #region Static factories

        public static CombinedSearchFilter Combined(params ISearchFilter[] filters)
        {
            var filter = new CombinedSearchFilter(filters)
            {
                Occurence = SearchFilterOccurence.Must
            };

            return filter;
        }

        public static CombinedSearchFilter Combined(string name, params ISearchFilter[] filters)
        {
            var filter = new CombinedSearchFilter(name, filters)
            {
                Occurence = SearchFilterOccurence.Must
            };

            return filter;
        }

        public static SearchFilter ByField(
            string fieldName,
            string term,
            SearchMode mode = SearchMode.Contains,
            bool escape = false,
            bool isNotAnalyzed = false)
        {
            Guard.NotEmpty(term);
            Guard.NotEmpty(fieldName);

            return new SearchFilter
            {
                FieldName = fieldName,
                Term = term,
                TypeCode = IndexTypeCode.String,
                Mode = mode,
                Escape = escape,
                IsNotAnalyzed = isNotAnalyzed
            };
        }

        public static SearchFilter ByField(string fieldName, int term)
        {
            return ByField(fieldName, term, IndexTypeCode.Int32);
        }

        public static SearchFilter ByField(string fieldName, bool term)
        {
            return ByField(fieldName, term, IndexTypeCode.Boolean);
        }

        public static SearchFilter ByField(string fieldName, double term)
        {
            return ByField(fieldName, term, IndexTypeCode.Double);
        }

        public static SearchFilter ByField(string fieldName, DateTime term)
        {
            return ByField(fieldName, term, IndexTypeCode.DateTime);
        }

        private static SearchFilter ByField(string fieldName, object term, IndexTypeCode typeCode)
        {
            Guard.NotEmpty(fieldName);

            return new SearchFilter
            {
                FieldName = fieldName,
                Term = term,
                TypeCode = typeCode
            };
        }


        public static RangeSearchFilter ByRange(string fieldName, string lower, string upper, bool includeLower = true, bool includeUpper = true)
        {
            return ByRange(fieldName, lower, upper, IndexTypeCode.String, includeLower, includeUpper);
        }

        public static RangeSearchFilter ByRange(string fieldName, int? lower, int? upper, bool includeLower = true, bool includeUpper = true)
        {
            return ByRange(fieldName, lower, upper, IndexTypeCode.Int32, includeLower, includeUpper);
        }

        public static RangeSearchFilter ByRange(string fieldName, double? lower, double? upper, bool includeLower = true, bool includeUpper = true)
        {
            return ByRange(fieldName, lower, upper, IndexTypeCode.Double, includeLower, includeUpper);
        }

        public static RangeSearchFilter ByRange(string fieldName, DateTime? lower, DateTime? upper, bool includeLower = true, bool includeUpper = true)
        {
            return ByRange(fieldName, lower, upper, IndexTypeCode.DateTime, includeLower, includeUpper);
        }

        private static RangeSearchFilter ByRange(
            string fieldName,
            object lowerTerm,
            object upperTerm,
            IndexTypeCode typeCode,
            bool includeLower,
            bool includeUpper)
        {
            Guard.NotEmpty(fieldName);

            return new RangeSearchFilter
            {
                FieldName = fieldName,
                Term = lowerTerm,
                UpperTerm = upperTerm,
                TypeCode = typeCode,
                IncludesLower = includeLower,
                IncludesUpper = includeUpper
            };
        }

        #endregion

        public override string ToString()
            => $"{FieldName}({Mode}):{Term}";
    }

    public enum SearchFilterOccurence
    {
        Must = 0,
        Should = 1,
        MustNot = 2
    }
}
