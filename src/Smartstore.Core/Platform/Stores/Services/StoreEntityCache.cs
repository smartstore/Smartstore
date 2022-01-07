using Smartstore.Data;

namespace Smartstore.Core.Stores
{
    public class StoreEntityCache
    {
        internal StoreEntityCache(IList<Store> allStores)
        {
            Guard.NotNull(allStores, nameof(allStores));

            Stores = allStores.ToDictionary(x => x.Id);
            var hostMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var store in allStores)
            {
                var hostValues = store.ParseHostValues();
                foreach (var host in hostValues)
                {
                    hostMap[host] = store.Id;
                }

                store.LazyLoader = NullLazyLoader.Instance;
            }

            if (allStores.Count > 0)
            {
                PrimaryStoreId = allStores.FirstOrDefault().Id;
            }

            HostMap = hostMap;
        }

        public IDictionary<int, Store> Stores { get; internal set; }
        public IDictionary<string, int> HostMap { get; internal set; }
        public int PrimaryStoreId { get; internal set; }

        public Store GetPrimaryStore()
        {
            return Stores.Get(PrimaryStoreId);
        }

        public Store GetStoreById(int id)
        {
            return Stores.Get(id);
        }

        public Store GetStoreByHostName(string host)
        {
            if (!string.IsNullOrEmpty(host) && HostMap.TryGetValue(host, out var id))
            {
                return Stores.Get(id);
            }

            return null;
        }
    }
}
