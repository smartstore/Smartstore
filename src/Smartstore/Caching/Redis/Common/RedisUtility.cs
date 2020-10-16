using System.Text;
using Smartstore.Data;

namespace Smartstore.Redis
{
    internal static class RedisUtility
    {
        // Prevents key collisions in multi-tenant/client environments
        //public static string ScopePrefix = string.Format("{0:x}", DataSettings.Instance.ConnectionString.Trim().ToLowerInvariant().Hash(Encoding.ASCII));
        //public static string ScopePrefix = DataSettings.Instance.ConnectionString.Trim().ToLowerInvariant().Hash(Encoding.ASCII);
        public static string ScopePrefix = DataSettings.Instance.TenantName.ToLowerInvariant();

        public static string BuildScopedKey(string key)
        {
            return ScopePrefix + ":" + key;
        }

        public static string GetEventFromChannel(string channel)
        {
            // Events come as: __keyevent@0__:[expire|evicted...]
            var index = channel.IndexOf(':');
            if (index >= 0 && index < channel.Length - 1)
                return channel[(index + 1)..];

            // We didn't find the delimeter, so just return the whole thing
            return channel;
        }

        public static bool TryParseEventMessage(string message, out string action, out string parameter)
        {
            action = null;
            parameter = null;

            if (message.IsEmpty())
            {
                return false;
            }

            var index = message.IndexOf('^');
            if (index >= 0 && index < message.Length - 1)
            {
                action = message.Substring(0, index);
                parameter = message[(index + 1)..];
            }
            else
            {
                action = message;
            }

            return true;
        }
    }
}