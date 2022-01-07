namespace Smartstore.Core.Stores
{
    public static class StoreExtensions
    {
        /// <summary>
        /// Gets the store host name
        /// </summary>
        /// <param name="store">The store to get the host name for</param>
        /// <param name="secure">
        /// If <c>null</c>, checks whether all pages should be secured per <see cref="Store.ForceSslForAllPages"/>.
        /// If <c>true</c>, returns the secure url, but only if SSL is enabled for the store.
        /// </param>
        /// <returns>The host name</returns>
        public static string GetHost(this Store store, bool? secure = null)
        {
            Guard.NotNull(store, nameof(store));

            return store.GetHost(secure ?? store.ForceSslForAllPages);
        }

        /// <summary>
        /// <c>true</c> if the store data is valid. Otherwise <c>false</c>.
        /// </summary>
        /// <param name="store">Store entity</param>
        public static bool IsStoreDataValid(this Store store)
        {
            Guard.NotNull(store, nameof(store));

            if (store.Url.IsEmpty())
                return false;

            try
            {
                var uri = new Uri(store.Url);
                var domain = uri.DnsSafeHost.EmptyNull().ToLower();

                switch (domain)
                {
                    case "www.yourstore.com":
                    case "yourstore.com":
                    case "www.mystore.com":
                    case "mystore.com":
                    case "www.mein-shop.de":
                    case "mein-shop.de":
                        return false;
                    default:
                        return store.Url.IsWebUrl();
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Indicates whether a store contains a specified host
        /// </summary>
        /// <param name="store">Store</param>
        /// <param name="host">Host</param>
        /// <returns>true - contains, false - no</returns>
        public static bool ContainsHostValue(this Store store, string host)
        {
            Guard.NotNull(store, nameof(store));

            if (string.IsNullOrEmpty(host))
            {
                return false;
            }

            var contains = store.ParseHostValues()
                                .FirstOrDefault(x => x.Equals(host, StringComparison.InvariantCultureIgnoreCase)) != null;
            return contains;
        }

        /// <summary>
        /// Parse comma-separated hosts
        /// </summary>
        /// <param name="store">Store</param>
        /// <returns>Comma-separated hosts</returns>
        public static string[] ParseHostValues(this Store store)
        {
            Guard.NotNull(store, nameof(store));

            if (string.IsNullOrWhiteSpace(store.Hosts))
            {
                return Array.Empty<string>();
            }

            return store.Hosts
                .Tokenize(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();
        }
    }
}