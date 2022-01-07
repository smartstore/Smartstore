namespace Smartstore.Core.Stores
{
    public static class IStoreContextExtensions
    {
        /// <summary>
        /// Gets all store entities from application (as untracked entities)
        /// </summary>
        /// <returns>Store collection</returns>
        public static ICollection<Store> GetAllStores(this IStoreContext context)
        {
            return context.GetCachedStores().Stores.Values;
        }

        /// <summary>
        /// Gets a store entity from application cache (as untracked entity)
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Store</returns>
        public static Store GetStoreById(this IStoreContext context, int storeId)
        {
            if (storeId == 0)
                return null;

            return context.GetCachedStores().GetStoreById(storeId);
        }

        /// <summary>
        /// <c>true</c> if only one store exists. Otherwise <c>false</c>.
        /// </summary>
        public static bool IsSingleStoreMode(this IStoreContext context)
        {
            return context.GetCachedStores().Stores.Count <= 1;
        }
    }
}
