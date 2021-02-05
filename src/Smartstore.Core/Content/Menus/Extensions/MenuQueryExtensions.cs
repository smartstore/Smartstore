using System.Linq;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Content.Menus
{
    public static partial class MenuQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts by <see cref="Menu.DisplayOrder"/>, then by <see cref="Menu.SystemName"/>, then by <see cref="Menu.Title"/>.
        /// </summary>
        /// <param name="includeHidden">Applies filter by <see cref="Menu.Published"/>.</param>
        /// <param name="groupBy">Groups by <see cref="Menu.Id"/> and applies filter to first result.</param>
        /// <param name="sort">Sorts by <see cref="Menu.DisplayOrder"/>, then by <see cref="Menu.SystemName"/>, then by <see cref="Menu.Title"/>.</param>
        public static IQueryable<Menu> ApplyStandardFilter(
            this IQueryable<Menu> query,
            bool includeHidden,
            bool groupBy = true,
            bool sort = true)
        {
            Guard.NotNull(query, nameof(query));
            
            query = query.Where(x => includeHidden || x.Published);

            if (groupBy)
            {
                query =
                    from x in query
                    group x by x.Id into grp
                    orderby grp.Key
                    select grp.FirstOrDefault();
            }

            if (sort)
            {
                query = query
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.SystemName)
                    .ThenBy(x => x.Title);
            }

            return query;
        }

        /// <summary>
        /// Applies filter by <see cref="Menu.Id"/> or <see cref="Menu.SystemName"/> and orders by <see cref="MenuItem.ParentItemId"/>, then by <see cref="MenuItem.DisplayOrder"/>.
        /// </summary>
        /// <param name="menuId">Applies filter by <see cref="Menu.Id"/>.</param>
        /// <param name="systemName">Applies filter by <see cref="Menu.SystemName"/>.</param>
        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
        /// <param name="includeHidden">Applies filter by <see cref="MenuItem.Published"/>.</param>
        /// <param name="customerRolesIds">Customer roles identifiers to apply filter by ACL restriction.</param>
        public static IQueryable<MenuItem> ApplyMenuFilter(
            this IQueryable<MenuItem> query,
            int menuId,
            string systemName,
            int storeId,
            bool includeHidden,
            int[] customerRolesIds = null)
        {
            var applied = false;
            var singleMenu = menuId != 0 || (systemName.HasValue() && storeId != 0);
            var db = query.GetDbContext<SmartDbContext>();
            var menuQuery = db.Menus
                .ApplyStoreFilter(storeId)
                .ApplyAclFilter(customerRolesIds)
                .Where(x => x.Id == menuId || x.SystemName == systemName)
                .ApplyStandardFilter(includeHidden, !singleMenu, !singleMenu);

            if (singleMenu)
            {
                menuQuery = menuQuery.Take(1);
            }

            query =
                from m in menuQuery
                join mi in db.MenuItems.AsNoTracking() on m.Id equals mi.MenuId
                where includeHidden || mi.Published
                orderby mi.ParentItemId, mi.DisplayOrder
                select mi;

            if (storeId > 0)
            {
                query = query.ApplyStoreFilter(storeId);
                applied = true;
            }

            if (customerRolesIds != null)
            {
                query = query.ApplyAclFilter(customerRolesIds);
                applied = true;
            }

            if (applied)
            {
                query =
                    from x in query
                    group x by x.Id into grp
                    orderby grp.Key
                    select grp.FirstOrDefault();
            }

            return query;
        }
    }
}
