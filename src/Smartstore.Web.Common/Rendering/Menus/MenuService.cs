using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Collections;
using Smartstore.Web.TagHelpers;

namespace Smartstore.Web.Rendering
{
    public partial class MenuService : IMenuService
    {
        protected readonly IMenuResolver[] _menuResolvers;

        public MenuService(IEnumerable<IMenuResolver> menuResolvers)
        {
            _menuResolvers = menuResolvers.OrderBy(x => x.Order).ToArray();
        }

        public virtual async Task<IMenu> GetMenuAsync(string name)
        {
            if (name.HasValue())
            {
                foreach (var resolver in _menuResolvers)
                {
                    if (await resolver.ExistsAsync(name))
                    {
                        return resolver.Resolve(name);
                    }
                }
            }

            return null;
        }

        public virtual async Task<TreeNode<MenuItem>> GetRootNodeAsync(string menuName)
        {
            var menu = await GetMenuAsync(menuName);
            return menu?.Root;
        }

        public virtual async Task ResolveElementCountsAsync(string menuName, TreeNode<MenuItem> curNode, bool deep = false)
        {
            var menu = await GetMenuAsync(menuName);
            menu?.ResolveElementCount(curNode, deep);
        }

        public virtual async Task ClearCacheAsync(string menuName)
        {
            var menu = await GetMenuAsync(menuName);
            menu?.ClearCache();
        }
    }
}
