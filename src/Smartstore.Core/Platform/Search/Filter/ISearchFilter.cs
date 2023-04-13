#nullable enable

namespace Smartstore.Core.Search
{
    public interface ISearchFilter
    {
        SearchFilterOccurence Occurence { get; }
        float Boost { get; }
    }

    public interface ICombinedSearchFilter : ISearchFilter
    {
        string? Name { get; }
        ICollection<ISearchFilter> Filters { get; }
    }

    public interface IAttributeSearchFilter : ISearchFilter
    {
        string FieldName { get; }
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
}
