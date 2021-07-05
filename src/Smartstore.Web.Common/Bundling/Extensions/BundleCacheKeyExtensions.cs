using System;

namespace Smartstore.Web.Bundling
{
    public static class BundleCacheKeyExtensions
    {
        public static bool IsValidationMode(this BundleCacheKey cacheKey)
            => cacheKey.Fragments?.ContainsKey("Validation") == true;

        public static string GetThemeName(this BundleCacheKey cacheKey)
            => cacheKey.Fragments?.Get("Theme");

        public static int? GetStoreId(this BundleCacheKey cacheKey)
            => cacheKey.Fragments?.Get("StoreId")?.Convert<int?>();
    }
}
