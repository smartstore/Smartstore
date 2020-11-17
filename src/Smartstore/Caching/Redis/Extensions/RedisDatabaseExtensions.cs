using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Smartstore.Redis
{
    public static class RedisDatabaseExtensions
    {
        public static T ObjectGet<T>(this IDatabase database, 
            IRedisSerializer serializer, 
            string key, 
            CommandFlags flags = CommandFlags.None)
        {
            var value = database.StringGet(key);

            if (value.IsNullOrEmpty)
            {
                return default;
            }

            if (!serializer.TryDeserialize<T>(value, true, out var result))
            {
                database.KeyDelete(key, flags);
            }

            return result;
        }

        public static async Task<T> ObjectGetAsync<T>(this IDatabase database, 
            IRedisSerializer serializer, 
            string key, 
            CommandFlags flags = CommandFlags.None)
        {
            var value = await database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                return default;
            }

            if (!serializer.TryDeserialize<T>(value, true, out var result))
            {
                await database.KeyDeleteAsync(key, flags);
            }

            return result;
        }

        public static bool ObjectSet(this IDatabase database,
            IRedisSerializer serializer,
            string key,
            object value,
            TimeSpan? expiry = null,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            Guard.NotEmpty(key, nameof(key));

            if (serializer.CanDeserialize(value?.GetType()) && serializer.TrySerialize(value, true, out var buffer))
            {
                return database.StringSet(key, buffer, expiry, when, flags);
            }

            return false;
        }

        public static Task<bool> ObjectSetAsync(this IDatabase database,
            IRedisSerializer serializer,
            string key,
            object value,
            TimeSpan? expiry = null,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            Guard.NotEmpty(key, nameof(key));

            if (serializer.CanDeserialize(value?.GetType()) && serializer.TrySerialize(value, true, out var buffer))
            {
                return database.StringSetAsync(key, buffer, expiry, when, flags);
            }

            return Task.FromResult(false);
        }
    }
}
