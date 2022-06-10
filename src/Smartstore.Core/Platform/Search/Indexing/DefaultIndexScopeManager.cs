namespace Smartstore.Core.Search.Indexing
{
    public class DefaultIndexScopeManager : IIndexScopeManager
    {
        private readonly IEnumerable<Lazy<IIndexScope, IndexScopeMetadata>> _scopes;

        public DefaultIndexScopeManager(IEnumerable<Lazy<IIndexScope, IndexScopeMetadata>> scopes)
        {
            _scopes = Guard.NotNull(scopes, nameof(scopes));
        }

        public IEnumerable<string> EnumerateScopes()
        {
            return _scopes.Select(x => x.Metadata.Name).OrderBy(x => x);
        }

        public IIndexScope GetIndexScope(string scope)
        {
            Guard.NotEmpty(scope, nameof(scope));

            var indexScope = _scopes.FirstOrDefault(x => x.Metadata.Name.EqualsNoCase(scope));
            if (indexScope == null)
            {
                throw new InvalidOperationException($"An index scope implementation for '{scope}' is not registered in the service container.");
            }

            return indexScope.Value;
        }
    }
}
