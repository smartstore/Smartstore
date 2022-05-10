namespace Smartstore.Core.Search.Indexing
{
    public class DefaultIndexScopeManager : IIndexScopeManager
    {
        private readonly Func<string, IIndexScope> _scopeFactory;

        public DefaultIndexScopeManager(Func<string, IIndexScope> scopeFactory)
        {
            _scopeFactory = Guard.NotNull(scopeFactory, nameof(scopeFactory));
        }

        public IIndexScope GetIndexScope(string scope)
        {
            Guard.NotEmpty(scope, nameof(scope));
            return _scopeFactory(scope) ?? throw new InvalidOperationException($"An index scope implementation for '{scope}' is not registered in the service container.");
        }
    }
}
