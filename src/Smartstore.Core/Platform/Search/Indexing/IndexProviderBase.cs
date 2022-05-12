namespace Smartstore.Core.Search.Indexing
{
    public abstract class IndexProviderBase : IIndexProvider
    {
        public virtual bool IsActive(string scope) => false;

        public abstract Task<IEnumerable<string>> EnumerateIndexesAsync();

        public virtual IIndexDocument CreateDocument(int id, string documentType)
            => new IndexDocument(id, documentType);

        public abstract IIndexStore GetIndexStore(string scope);

        public abstract ISearchEngine GetSearchEngine(IIndexStore store, ISearchQuery query);
    }
}
