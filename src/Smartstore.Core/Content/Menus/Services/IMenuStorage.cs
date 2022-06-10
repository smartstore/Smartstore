using Smartstore.Caching;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Content.Menus
{
    public partial interface IMenuStorage
    {
        /// <summary>
        /// Gets cached infos about all user menus.
        /// </summary>
        /// <param name="roles">Customer roles to check access for. <c>null</c> to use current customer's roles.</param>
        /// <param name="storeId">Store identifier. 0 to use current store.</param>
        /// <returns>Menu infos.</returns>
        Task<IEnumerable<MenuInfo>> GetUserMenuInfosAsync(IEnumerable<CustomerRole> roles = null, int storeId = 0);

        /// <summary>
        /// Checks whether the menu exists.
        /// </summary>
        /// <param name="systemName">Menu system name <see cref="MenuEntity.SystemName"/>.</param>
        /// <returns><c>true</c> the menu exists, <c>false</c> the menu doesn't exist.</returns>
        Task<bool> MenuExistsAsync(string systemName);

        /// <summary>
        /// Gets the system names of all published menus.
        /// </summary>
        /// <param name="ensureCreated">Bypasses cache and returns entities from database.</param>
        Task<ISet> GetMenuSystemNamesAsync(bool ensureCreated);

        /// <summary>
        /// Deletes a menu item.
        /// </summary>
        /// <param name="item">Menu item entity.</param>
        /// <param name="deleteChilds">Specifies whether to delete all child items too.</param>
        Task DeleteMenuItemAsync(MenuItemEntity item, bool deleteChilds = true);
    }
}
