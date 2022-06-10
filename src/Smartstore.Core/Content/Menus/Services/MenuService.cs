using Smartstore.Collections;

namespace Smartstore.Core.Content.Menus
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
            if (menu != null)
            {
                return await menu.GetRootNodeAsync();
            }

            return null;
        }

        public virtual async Task ResolveElementCountsAsync(string menuName, TreeNode<MenuItem> curNode, bool deep = false)
        {
            var menu = await GetMenuAsync(menuName);
            if (menu != null)
            {
                await menu.ResolveElementCountAsync(curNode, deep);
            }
        }

        public virtual async Task ClearCacheAsync(string menuName)
        {
            (await GetMenuAsync(menuName))?.ClearCacheAsync();
        }
    }
}
