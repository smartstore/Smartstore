#nullable enable

using Smartstore.Utilities;

namespace Smartstore.Core.Search
{
    public interface ISearchFilter
    {
        SearchFilterOccurence Occurence { get; }
        float Boost { get; }
    }

    public interface INamedSearchFilter : ISearchFilter
    {
        string FieldName { get; }
    }

    public interface ICombinedSearchFilter : INamedSearchFilter
    {
        ICollection<ISearchFilter> Filters { get; }
    }

    public interface IAttributeSearchFilter : INamedSearchFilter
    {
        IndexTypeCode TypeCode { get; }
        object? Term { get; }
        public SearchMode Mode { get; }
        bool IsNotAnalyzed { get; }
        int ParentId { get; }
    }

    public interface IRangeSearchFilter : IAttributeSearchFilter
    {
        object? UpperTerm { get; }
        bool IncludesLower { get; }
        bool IncludesUpper { get; }
    }

    public static class ISearchFilterExtensions
    {
        public static T[] GetTermsArray<T>(this INamedSearchFilter filter)
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
