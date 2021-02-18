using System;
using Smartstore.Utilities;

namespace Smartstore.Core.Content.Menus
{
    public class MenuItem : NavigationItem, ICloneable<MenuItem>
    {
        private string _id;

        /// <summary>
        /// If this menu item refers to an entity, the id of the backed entity (like category, products e.g.)
        /// </summary>
        public int EntityId { get; set; }
        public string EntityName { get; set; }

        public int MenuItemId { get; set; }

        /// <summary>
        /// The total count of contained elements (like the count of products within a category)
        /// </summary>
        public int? ElementsCount { get; set; }

        /// <summary>
        /// Unique identifier.
        /// </summary>
        public string Id
        {
            get
            {
                if (_id == null)
                {
                    _id = CommonHelper.GenerateRandomDigitCode(10).TrimStart('0');
                }

                return _id;
            }
            set => _id = value;
        }

        public string ResKey { get; set; }

        public string PermissionNames { get; set; }

        public bool IsGroupHeader { get; set; }

        // TODO: (mh) (core) Implement MenuItemBuilder
        //public MenuItemBuilder ToBuilder()
        //{
        //    return new MenuItemBuilder(this);
        //}

        //public static implicit operator MenuItemBuilder(MenuItem menuItem)
        //{
        //    return menuItem.ToBuilder();
        //}

        public MenuItem Clone()
            => (MenuItem)MemberwiseClone();

        object ICloneable.Clone()
            => Clone();
    }
}
