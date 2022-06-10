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
