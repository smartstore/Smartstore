using System.Collections.Generic;

namespace Smartstore.Core.Content.Menus
{
    public class MenuItemBuilder : NavigationItemBuilder<MenuItem, MenuItemBuilder>
    {
        private readonly IList<string> _permissionNames;

        public MenuItemBuilder(MenuItem item)
            : base(item)
        {
            _permissionNames = new List<string>();
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

        public MenuItemBuilder PermissionNames(string value)
        {
            Item.PermissionNames = value;
            return this;
        }

        public MenuItemBuilder ResKey(string value)
        {
            Item.ResKey = value;
            return this;
        }

        public static implicit operator MenuItem(MenuItemBuilder builder)
        {
            return builder.ToItem();
        }
    }
}
