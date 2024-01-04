namespace Smartstore.Core.Content.Menus
{
    internal class DependencyMenuResolver : IMenuResolver
    {
        private readonly Dictionary<string, IMenu> _menus;

        public DependencyMenuResolver(IEnumerable<IMenu> menus)
        {
            _menus = menus.ToDictionarySafe(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);
        }

        public int Order => 0;

        public Task<bool> ExistsAsync(string menuName)
        {
            Guard.NotEmpty(menuName);

            return Task.FromResult(_menus.ContainsKey(menuName));
        }

        public IMenu Resolve(string menuName)
        {
            _menus.TryGetValue(menuName, out var menu);
            return menu;
        }
    }
}
