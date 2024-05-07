#nullable enable

using Smartstore.Core.Rules;
using Smartstore.Utilities;

namespace Smartstore.Core.Search
{
    public interface ISearchFilter
    {
        string FieldName { get; }
        SearchFilterOccurence Occurence { get; }
        float? Boost { get; }
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
    }

    public interface IRangeSearchFilter : IAttributeSearchFilter
    {
        object? UpperTerm { get; }
        bool IncludesLower { get; }
        bool IncludesUpper { get; }
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
                return [convertedValue!];
            }

            return [];
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

        public static RuleOperator GetOperator(this IAttributeSearchFilter filter)
        {
            Guard.NotNull(filter);

            var negate = filter!.Occurence == SearchFilterOccurence.MustNot;

            switch (filter.Mode)
            {
                case SearchMode.StartsWith:
                    return RuleOperator.StartsWith;
                case SearchMode.Contains:
                    return negate ? RuleOperator.NotContains : RuleOperator.Contains;
                case SearchMode.ExactMatch:
                default:
                    return negate ? RuleOperator.IsNotEqualTo : RuleOperator.IsEqualTo;
            }
        }
    }
}
