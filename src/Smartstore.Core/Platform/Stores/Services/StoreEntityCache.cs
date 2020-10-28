using System;
using System.Collections.Generic;

namespace Smartstore.Core.Stores
{
    public class StoreEntityCache
    {
        public IDictionary<int, Store> Stores { get; set; }
        public IDictionary<string, int> HostMap { get; set; }
        public int PrimaryStoreId { get; set; }

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
