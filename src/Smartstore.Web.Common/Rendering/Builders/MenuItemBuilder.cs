using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
