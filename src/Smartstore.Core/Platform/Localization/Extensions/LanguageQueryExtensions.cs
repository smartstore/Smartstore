using Smartstore.Core.Stores;

namespace Smartstore.Core.Localization
{
    public static partial class LanguageQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts by <see cref="IDisplayOrder.DisplayOrder"/>.
        /// </summary>
        /// <param name="query">Language query.</param>
        /// <param name="includeHidden">Applies filter by <see cref="Language.Published"/>.</param>
        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
        /// <returns>Language query.</returns>
        public static IOrderedQueryable<Language> ApplyStandardFilter(this IQueryable<Language> query, bool includeHidden = false, int storeId = 0)
        {
            Guard.NotNull(query, nameof(query));

            if (!includeHidden)
            {
                query = query.Where(x => x.Published);
            }

            if (storeId > 0)
            {
                query = query.ApplyStoreFilter(storeId);
            }

            return query.OrderBy(x => x.DisplayOrder);
        }
    }
}