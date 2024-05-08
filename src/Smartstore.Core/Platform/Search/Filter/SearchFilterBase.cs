namespace Smartstore.Core.Search
{
    public abstract class SearchFilterBase : ISearchFilter
    {
        protected SearchFilterBase()
        {
            Occurence = SearchFilterOccurence.Should;
        }

        public string FieldName
        {
            get;
            protected internal set;
        }

        public float? Boost
        {
            get;
            protected internal set;
        }

        public SearchFilterOccurence Occurence
        {
            get;
            protected internal set;
        }
    }
}
