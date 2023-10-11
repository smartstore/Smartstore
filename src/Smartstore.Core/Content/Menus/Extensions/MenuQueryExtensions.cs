using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Content.Menus
{
    public static partial class MenuQueryExtensions
    {
        /// <summary>
        /// Applies standard filter for <see cref="MenuEntity"/>.
        /// </summary>
        /// <param name="systemName">Applies filter by <see cref="MenuEntity.SystemName"/>.</param>
        /// <param name="isSystemMenu">Applies filter by <see cref="MenuEntity.IsSystemMenu"/>.</param>
        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
        /// <param name="customerRoleIds">Customer roles identifiers to apply filter by ACL restriction.</param>
        /// <param name="includeHidden">Applies filter by <see cref="MenuEntity.Published"/>.</param>
        /// <returns><see cref="MenuEntity"/> query.</returns>
        public static IQueryable<MenuEntity> ApplyStandardFilter(this IQueryable<MenuEntity> query,
            string systemName = null,
            bool? isSystemMenu = null,
            int storeId = 0,
            int[] customerRoleIds = null,
            bool includeHidden = false)
        {
            Guard.NotNull(query);

            if (systemName.HasValue())
            {
                query = query.Where(x => x.SystemName == systemName);
            }

            if (isSystemMenu.HasValue)
            {
                query = query.Where(x => x.IsSystemMenu == isSystemMenu.Value);
            }

            if (!includeHidden)
            {
                query = query.Where(x => x.Published);
            }

            // INFO: we do not apply sorting here because it is lost when using ApplyMenuItemFilter.

            return query
                .ApplyStoreFilter(storeId)
                .ApplyAclFilter(customerRoleIds);
        }

        /// <summary>
        /// Applies filter for <see cref="MenuItemEntity"/>.
        /// </summary>
        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
        /// <param name="customerRoleIds">Customer roles identifiers to apply filter by ACL restriction.</param>
        /// <param name="includeHidden">Applies filter by <see cref="MenuItemEntity.Published"/>.</param>
        /// <returns><see cref="MenuItemEntity"/> query.</returns>
        public static IOrderedQueryable<MenuItemEntity> ApplyMenuItemFilter(this IQueryable<MenuEntity> query,
            int storeId = 0,
            int[] customerRoleIds = null,
            bool includeHidden = false)
        {
            Guard.NotNull(query);

            var db = query.GetDbContext<SmartDbContext>();

            var itemQuery =
                from m in query
                join mi in db.MenuItems on m.Id equals mi.MenuId
                where includeHidden || mi.Published
                select mi;

            itemQuery = itemQuery
                .ApplyStoreFilter(storeId)
                .ApplyAclFilter(customerRoleIds);

            return itemQuery
                .OrderBy(x => x.ParentItemId)
                .ThenBy(x => x.DisplayOrder);
        }

        /// <summary>
        /// Applies order by <see cref="MenuEntity.DisplayOrder"/>, then by <see cref="MenuEntity.SystemName"/>,
        /// then by <see cref="MenuEntity.Title"/>.
        /// </summary>
        public static IOrderedQueryable<MenuEntity> ApplySorting(this IQueryable<MenuEntity> query)
        {
            return query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.SystemName)
                .ThenBy(x => x.Title);
        }
    }
}
