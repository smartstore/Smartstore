using Smartstore.Caching;
using Smartstore.Collections;

namespace Smartstore.Core.Content.Menus
{
    public interface IMenuPublisher
    {
        void RegisterMenus(TreeNode<MenuItem> rootNode, string menuName);
    }

    public class MenuPublisher : IMenuPublisher
    {
        private readonly ITypeScanner _typeScanner;
        private readonly IRequestCache _requestCache;

        public MenuPublisher(ITypeScanner typeScanner, IRequestCache requestCache)
        {
            _typeScanner = typeScanner;
            _requestCache = requestCache;
        }

        public void RegisterMenus(TreeNode<MenuItem> rootNode, string menuName)
        {
            Guard.NotNull(rootNode, nameof(rootNode));
            Guard.NotEmpty(menuName, nameof(menuName));

            var providers = _requestCache.Get("sm.menu.providers.{0}".FormatInvariant(menuName), () =>
            {
                var allInstances = _requestCache.Get("sm.menu.allproviders", () =>
                {
                    var instances = new List<IMenuProvider>();
                    var providerTypes = _typeScanner.FindTypes<IMenuProvider>();

                    foreach (var type in providerTypes)
                    {
                        try
                        {
                            var provider = EngineContext.Current.Scope.ResolveUnregistered(type) as IMenuProvider;
                            instances.Add(provider);
                        }
                        catch
                        {
                        }
                    }

                    return instances;
                });

                return allInstances.Where(x => x.MenuName.EqualsNoCase(menuName)).OrderBy(x => x.Ordinal).ToList();
            });

            providers.Each(x => x.BuildMenu(rootNode));
        }
    }
}
