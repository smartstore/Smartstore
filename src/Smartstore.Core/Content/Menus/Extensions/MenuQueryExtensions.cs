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
        /// Applies standard filter and optionally sorts by <see cref="MenuEntity.DisplayOrder"/>, then by <see cref="MenuEntity.SystemName"/>, then by <see cref="MenuEntity.Title"/>.
        /// </summary>
        /// <param name="includeHidden">Applies filter by <see cref="MenuEntity.Published"/>.</param>
        /// <param name="groupBy">Groups by <see cref="MenuEntity.Id"/> and applies filter to first result.</param>
        /// <param name="sort">Sorts by <see cref="MenuEntity.DisplayOrder"/>, then by <see cref="MenuEntity.SystemName"/>, then by <see cref="MenuEntity.Title"/>.</param>
        public static IQueryable<MenuEntity> ApplyStandardFilter(this IQueryable<MenuEntity> query,
            bool includeHidden,
            bool groupBy = true,
            bool sort = true)
        {
            Guard.NotNull(query, nameof(query));
            
            query = query.Where(x => includeHidden || x.Published);

            if (groupBy)
            {
                // TODO: (mh) (core) Grouping does not work with efcore5 anymore. Use distinct()?
                query = query.Distinct();

                //query =
                //    from x in query
                //    group x by x.Id into grp
                //    orderby grp.Key
                //    select grp.FirstOrDefault();
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
        /// Applies filter by <see cref="MenuEntity.Id"/> or <see cref="MenuEntity.SystemName"/> and orders by <see cref="MenuItemEntity.ParentItemId"/>, then by <see cref="MenuItemEntity.DisplayOrder"/>.
        /// </summary>
        /// <param name="menuId">Applies filter by <see cref="MenuEntity.Id"/>.</param>
        /// <param name="systemName">Applies filter by <see cref="MenuEntity.SystemName"/>.</param>
        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
        /// <param name="includeHidden">Applies filter by <see cref="MenuItemEntity.Published"/>.</param>
        /// <param name="customerRoleIds">Customer roles identifiers to apply filter by ACL restriction.</param>
        public static IQueryable<MenuItemEntity> ApplyMenuFilter(this IQueryable<MenuItemEntity> query,
            int menuId,
            string systemName,
            int storeId = 0,
            bool includeHidden = false,
            int[] customerRoleIds = null)
        {
            // TODO: (mh) (core) Revise this code THOROUGHLY!! It seems broken. The flow is not the same. Compare with source!
            
            var applied = false;
            var singleMenu = menuId != 0 || (systemName.HasValue() && storeId != 0);
            var db = query.GetDbContext<SmartDbContext>();

            var menuQuery = db.Menus.ApplyStandardFilter(includeHidden, !singleMenu, !singleMenu);

            if (menuId != 0)
            {
                menuQuery = menuQuery.Where(x => x.Id == menuId);
            }

            if (systemName.HasValue())
            {
                menuQuery = menuQuery.Where(x => x.SystemName == systemName);
            }

            if (singleMenu)
            {
                menuQuery = menuQuery.Take(1);
            }

            query =
                from m in menuQuery
                join mi in db.MenuItems.Include(x => x.Menu).AsNoTracking() on m.Id equals mi.MenuId
                where includeHidden || mi.Published
                orderby mi.ParentItemId, mi.DisplayOrder
                select mi;

            if (storeId > 0)
            {
                query = query.ApplyStoreFilter(storeId);
                applied = true;
            }

            if (customerRoleIds != null)
            {
                query = query.ApplyAclFilter(customerRoleIds);
                applied = true;
            }

            if (applied)
            {
                // TODO: (mh) (core) Grouping does not work with efcore5 anymore. Use distinct()?
                query = query.Distinct();

                //query =
                //    from x in query
                //    group x by x.Id into grp
                //    orderby grp.Key
                //    select grp.FirstOrDefault();
            }

            return query;
        }
    }
}
