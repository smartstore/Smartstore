using Smartstore.Data;

namespace Smartstore.Redis
{
    internal static class RedisUtility
    {
        // Prevents key collisions in multi-tenant/client environments
        public static string ScopePrefix = string.Format("{0:x}", DataSettings.Instance.ConnectionString.Trim().ToLowerInvariant().GetHashCode());

        public static string BuildScopedKey(string key)
        {
            return ScopePrefix + ":" + key;
        }

        public static string GetEventFromChannel(string channel)
        {
            // Events come as: __keyevent@0__:[expire|evicted...]
            var index = channel.IndexOf(':');
            if (index >= 0 && index < channel.Length - 1)
                return channel.Substring(index + 1);

            // We didn't find the delimeter, so just return the whole thing
            return channel;
        }
    }
}