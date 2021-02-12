using System.Threading.Tasks;
using Autofac;
using Smartstore.Core.Content.Menus;

namespace Smartstore.Web.Rendering
{
    public class DatabaseMenuResolver : IMenuResolver
    {
        protected readonly IComponentContext _ctx;
        protected readonly IMenuStorage _menuStorage;

        public DatabaseMenuResolver(IComponentContext ctx, IMenuStorage menuStorage)
        {
            _ctx = ctx;
            _menuStorage = menuStorage;
        }

        public int Order => 1;

        public async Task<bool> ExistsAsync(string menuName)
        {
            return await _menuStorage.MenuExistsAsync(menuName);
        }

        public IMenu Resolve(string name)
        {
            var menu = _ctx.ResolveNamed<IMenu>("database", new NamedParameter("menuName", name));
            return menu;
        }
    }
}
