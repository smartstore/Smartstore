using Smartstore.Collections;
using Smartstore.Utilities;

namespace Smartstore.Core.Content.Menus
{
    public class MenuItem : NavigationItem, IKeyedNode, ICloneable<MenuItem>
    {
        private string _id;

        /// <inheritdoc/>
        object IKeyedNode.GetNodeKey()
        {
            return Id;
        }

        /// <summary>
        /// If this menu item refers to an entity, the id of the backed entity (like category, products e.g.)
        /// </summary>
        public int EntityId { get; set; }
        public string EntityName { get; set; }

        /// <summary>
        /// If this menu item originates from the database, the id of the <see cref="MenuItemEntity"/> entity.
        /// </summary>
        public int MenuItemId { get; set; }

        /// <summary>
        /// If this menu item originates from the database, the id of the containing <see cref="MenuEntity"/> entity.
        /// </summary>
        public int MenuId { get; set; }

        /// <summary>
        /// The total count of contained elements (like the count of products within a category)
        /// </summary>
        public int? ElementsCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="ElementsCount"/> has been resolved already.
        /// </summary>
        public bool ElementsCountResolved { get; set; }

        /// <summary>
        /// Unique identifier.
        /// </summary>
        public string Id
        {
            get => _id ??= CommonHelper.GenerateRandomDigitCode(10).TrimStart('0');
            set => _id = value;
        }

        public string ResKey { get; set; }

        public string PermissionNames { get; set; }

        public bool IsGroupHeader { get; set; }

        public MenuItem Clone()
            => (MenuItem)MemberwiseClone();

        object ICloneable.Clone()
            => Clone();
    }
}
