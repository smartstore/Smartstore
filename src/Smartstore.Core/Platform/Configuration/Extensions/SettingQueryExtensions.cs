namespace Smartstore.Core.Configuration
{
    public static class SettingQueryExtensions
    {
        /// <summary>
        /// Applies order by <see cref="Setting.Name"/>, then by <see cref="Setting.StoreId"/>
        /// </summary>
        public static IOrderedQueryable<Setting> ApplySorting(this IQueryable<Setting> source)
        {
            return source.OrderBy(x => x.Name).ThenBy(x => x.StoreId);
        }

        /// <summary>
        /// Gets all settings for given type <paramref name="settingsType"/> and <paramref name="storeId"/>.
        /// Type must implement <see cref="ISettings"/>.
        /// </summary>
        /// <param name="doFallback">
        /// Whether any store-neutral settings (Setting.StoreId = 0) should be fetched if store-specific entry does not exist.
        /// </param>
        public static IQueryable<Setting> ApplyClassFilter(this IQueryable<Setting> source, Type settingsType, int storeId, bool doFallback = false)
        {
            var prefix = settingsType.Name + ".";

            var query = source.Where(x => x.Name.StartsWith(prefix));

            if (doFallback && storeId > 0)
            {
                query = query.Where(x => x.StoreId == 0 || x.StoreId == storeId);
            }
            else
            {
                query = query.Where(x => x.StoreId == storeId);
            }

            return query;
        }
    }
}
