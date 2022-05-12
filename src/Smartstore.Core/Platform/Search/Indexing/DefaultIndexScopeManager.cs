namespace Smartstore.Core.Search.Indexing
{
    public class DefaultIndexScopeManager : IIndexScopeManager
    {
        private readonly IEnumerable<Lazy<IIndexScope, IndexScopeMetadata>> _scopes;
        private readonly Func<string, IIndexScope> _scopeFactory;

        public DefaultIndexScopeManager(
            IEnumerable<Lazy<IIndexScope, IndexScopeMetadata>> scopes,
            Func<string, IIndexScope> scopeFactory)
        {
            _scopes = Guard.NotNull(scopes, nameof(scopes));
            _scopeFactory = Guard.NotNull(scopeFactory, nameof(scopeFactory));
        }

        public IEnumerable<string> EnumerateScopes()
        {
            return _scopes.Select(x => x.Metadata.Name).OrderBy(x => x);
        }

        public IIndexScope GetIndexScope(string scope)
        {
            Guard.NotEmpty(scope, nameof(scope));
            return _scopeFactory(scope) ?? throw new InvalidOperationException($"An index scope implementation for '{scope}' is not registered in the service container.");
        }
    }
}
