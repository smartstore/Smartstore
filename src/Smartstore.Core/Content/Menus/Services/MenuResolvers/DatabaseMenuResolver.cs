using Autofac;

namespace Smartstore.Core.Content.Menus
{
    internal class DatabaseMenuResolver : IMenuResolver
    {
        protected readonly IComponentContext _ctx;
        protected readonly IMenuStorage _menuStorage;

        public DatabaseMenuResolver(IComponentContext ctx, IMenuStorage menuStorage)
        {
            _ctx = ctx;
            _menuStorage = menuStorage;
        }

        public int Order => 1;

        public Task<bool> ExistsAsync(string menuName)
            => _menuStorage.MenuExistsAsync(menuName);

        public IMenu Resolve(string name)
        {
            var menu = _ctx.ResolveNamed<IMenu>("database", new NamedParameter("menuName", name));
            return menu;
        }
    }
}
