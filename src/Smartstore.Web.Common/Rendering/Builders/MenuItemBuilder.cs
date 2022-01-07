using Smartstore.Core.Content.Menus;

namespace Smartstore.Web.Rendering.Builders
{
    public class MenuItemBuilder : NavigationItemBuilder<MenuItem, MenuItemBuilder>
    {
        public MenuItemBuilder(MenuItem item)
            : base(item)
        {
        }

        public MenuItemBuilder Id(string value)
        {
            Item.Id = value;
            return this;
        }

        public MenuItemBuilder IsGroupHeader(bool value)
        {
            Item.IsGroupHeader = value;
            return this;
        }

        public MenuItemBuilder PermissionNames(params string[] names)
        {
            Item.PermissionNames = string.Join(',', names.Select(x => x.Trim()));
            return this;
        }

        public MenuItemBuilder ResKey(string value)
        {
            Item.ResKey = value;
            return this;
        }

        public static implicit operator MenuItem(MenuItemBuilder builder)
            => builder.AsItem();
    }

    public static class MenuItemExtensions
    {
        public static MenuItemBuilder ToBuilder(this MenuItem item)
        {
            Guard.NotNull(item, nameof(item));
            return new MenuItemBuilder(item);
        }
    }
}
