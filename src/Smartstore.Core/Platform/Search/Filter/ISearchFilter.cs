#nullable enable

using Smartstore.Utilities;

namespace Smartstore.Core.Search
{
    public interface ISearchFilter
    {
        string FieldName { get; }
        SearchFilterOccurence Occurence { get; }
        float Boost { get; }
    }

    public interface ICombinedSearchFilter : ISearchFilter
    {
        ICollection<ISearchFilter> Filters { get; }
    }

    public interface IAttributeSearchFilter : ISearchFilter
    {
        IndexTypeCode TypeCode { get; }
        object? Term { get; }
        SearchMode Mode { get; }
        bool IsNotAnalyzed { get; }
        int ParentId { get; }

        //bool IsTextual
        //    => TypeCode == IndexTypeCode.String;
    }

    public interface IRangeSearchFilter : IAttributeSearchFilter
    {
        object? UpperTerm { get; }
        bool IncludesLower { get; }
        bool IncludesUpper { get; }
    }

    // TODO: (mg) should we use such a new interface for term filters to distinguish more easily? I guess so:
    public interface ISearchTermFilter : ISearchFilter
    {
        string? Term { get; }
        SearchMode Mode { get; }
        bool Escape { get; }
        bool IsAnalyzed { get; }
    }

    // TODO: (mg) move to new file when ready.
    public class SearchTermFilter : SearchFilterBase, ISearchTermFilter
    {
        public string? Term
        {
            get;
            protected internal set;
        }

        public SearchMode Mode
        {
            get;
            protected internal set;
        }

        public bool Escape
        {
            get;
            protected internal set;
        }

        public bool IsAnalyzed
        {
            get;
            protected internal set;
        }
    }

    public static class ISearchFilterExtensions
    {
        /// <summary>
        /// Searches for a filter including <see cref="ICombinedSearchFilter"/>.
        /// </summary>
        public static ISearchFilter? FindFilter(this IEnumerable<ISearchFilter> filters, string? fieldName)
        {
            Guard.NotNull(filters);
            
            if (fieldName.IsEmpty())
            {
                return null;
            }
            
            foreach (var filter in filters)
            {
                if (filter is IAttributeSearchFilter attrFilter && attrFilter.FieldName == fieldName)
                {
                    return attrFilter;
                }

                if (filter is ICombinedSearchFilter combinedFilter)
                {
                    var filter2 = FindFilter(combinedFilter.Filters, fieldName);
                    if (filter2 != null)
                    {
                        return filter2;
                    }
                }
            }

            return null;
        }

        public static T[] GetTermsArray<T>(this ISearchFilter filter)
        {
            Guard.NotNull(filter);

            if (filter is ICombinedSearchFilter combinedFilter)
            {
                return combinedFilter.GetTermsArray<T>();
            }
            else if (filter is IAttributeSearchFilter attrFilter && ConvertUtility.TryConvert<T>(attrFilter.Term, out var convertedValue))
            {
                return new T[] { convertedValue! };
            }

            return Array.Empty<T>();
        }

        public static T[] GetTermsArray<T>(this ICombinedSearchFilter filter)
        {
            Guard.NotNull(filter);

            var terms = filter.Filters
                .OfType<IAttributeSearchFilter>()
                .Select(x => x.Term)
                .OfType<T>()
                .ToArray();

            return terms;
        }
    }
}
