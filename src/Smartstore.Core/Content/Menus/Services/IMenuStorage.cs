using Smartstore.Caching;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Content.Menus
{
    public partial interface IMenuStorage
    {
        /// <summary>
        /// Gets cached infos about all user menus.
        /// </summary>
        /// <param name="roles">Customer roles to check access for. <c>null</c> to use current customer's roles.</param>
        /// <param name="storeId">Store identifier. If <c>null</c>, identifier will be obtained via <see cref="IStoreContext.CurrentStore"/>.</param>
        /// <returns>Menu infos.</returns>
        Task<IEnumerable<MenuInfo>> GetUserMenuInfosAsync(IEnumerable<CustomerRole> roles = null, int? storeId = null);

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
        /// <param name="storeId">Store identifier. If <c>null</c>, identifier will be obtained via <see cref="IStoreContext.CurrentStore"/>.</param>
        Task<ISet> GetMenuSystemNamesAsync(bool ensureCreated, int? storeId = null);

        /// <summary>
        /// Deletes a menu item.
        /// </summary>
        /// <param name="item">Menu item entity.</param>
        /// <param name="deleteChilds">Specifies whether to delete all child items too.</param>
        Task DeleteMenuItemAsync(MenuItemEntity item, bool deleteChilds = true);
    }
}
