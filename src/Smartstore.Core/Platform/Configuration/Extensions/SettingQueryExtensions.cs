using System;
using System.Linq;
using Smartstore.Core.Configuration;

namespace Smartstore
{
    public static class SettingQueryExtensions
    {
        /// <summary>
        /// Applies order by <see cref="Setting.Name"/>, then by <see cref="Setting.StoreId"/>
        /// </summary>
        public static IOrderedQueryable<Setting> ApplySorting(this IQueryable<Setting> query)
        {
            return query.OrderBy(x => x.Name).ThenBy(x => x.StoreId);
        }
    }
}
