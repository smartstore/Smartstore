using Autofac;

namespace Smartstore.Core.Search.Indexing
{
    public interface IIndexScopeProvider
    {
        IIndexCollector GetCollector(string scope);
        ISearchProvider GetSearchProvider(string scope);
        IIndexAnalyzer GetAnalyzer(string scope);
    }

    public class DefaultIndexScopeProvider : IIndexScopeProvider
    {
        private readonly IComponentContext _componentContext;
        private readonly IEnumerable<IIndexCollector> _collectors;

        public DefaultIndexScopeProvider(IComponentContext componentContext, IEnumerable<IIndexCollector> collectors)
        {
            _componentContext = componentContext;
            _collectors = collectors;
        }

        public IIndexCollector GetCollector(string scope)
        {
            Guard.NotEmpty(scope, nameof(scope));

            return _collectors.FirstOrDefault(x => x.Scope.EqualsNoCase(scope));
        }

        public ISearchProvider GetSearchProvider(string scope)
        {
            Guard.NotEmpty(scope, nameof(scope));

            return _componentContext.ResolveNamed<ISearchProvider>(scope);
        }

        public IIndexAnalyzer GetAnalyzer(string scope)
        {
            Guard.NotEmpty(scope, nameof(scope));

            return _componentContext.ResolveNamed<IIndexAnalyzer>(scope);
        }
    }
}
