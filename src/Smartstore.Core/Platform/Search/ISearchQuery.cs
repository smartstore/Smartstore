using Smartstore.Core.Search.Facets;

namespace Smartstore.Core.Search
{
    public interface ISearchQuery
    {
        // Language, Currency & Store
        int? LanguageId { get; }
        string LanguageCulture { get; }
        string CurrencyCode { get; }
        int? StoreId { get; }

        // Search term
        string[] Fields { get; }
        string Term { get; }
        SearchMode Mode { get; }
        bool EscapeTerm { get; }
        bool IsFuzzySearch { get; }

        // Filtering
        ICollection<ISearchFilter> Filters { get; }

        // Facets
        IReadOnlyDictionary<string, FacetDescriptor> FacetDescriptors { get; }

        // Paging
        int Skip { get; }
        int Take { get; }

        // Sorting
        ICollection<SearchSort> Sorting { get; }

        /// <summary>
        /// Maximum number of suggestions returned from spell checker
        /// </summary>
        int SpellCheckerMaxSuggestions { get; }

        /// <summary>
        /// Defines how many characters must be in the query before suggestions are provided
        /// </summary>
        int SpellCheckerMinQueryLength { get; }

        /// <summary>
        /// The maximum number of product hits up to which suggestions are provided
        /// </summary>
        int SpellCheckerMaxHitCount { get; }

        // Misc
        string Origin { get; }
        IDictionary<string, object> CustomData { get; }
    }

    public static class ISearchQueryExtensions
    {
        /// <summary>
        /// Gets a value indicating whether the origin is instant search.
        /// </summary>
        public static bool IsInstantSearch(this ISearchQuery query)
            => query?.Origin?.EndsWith("/InstantSearch", StringComparison.OrdinalIgnoreCase) ?? false;

        /// <summary>
        /// Gets a value indicating whether the origin is the search page.
        /// </summary>
        public static bool IsSearchPage(this ISearchQuery query)
            => query?.Origin?.EndsWith("/Search", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
