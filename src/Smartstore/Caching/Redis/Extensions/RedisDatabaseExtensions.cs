using System;
using System.Threading.Tasks;
using Smartstore.ComponentModel;
using StackExchange.Redis;

namespace Smartstore.Redis
{
    public static class RedisDatabaseExtensions
    {
        public static T ObjectGet<T>(this IDatabase database, 
            IJsonSerializer serializer, 
            string key,
            bool uncompress = true,
            CommandFlags flags = CommandFlags.None)
        {
            var value = database.StringGet(key);

            if (value.IsNullOrEmpty)
            {
                return default;
            }

            if (!serializer.TryDeserialize(typeof(T), value, uncompress, out var result))
            {
                database.KeyDelete(key, flags);
            }

            return (T)result;
        }

        public static async Task<T> ObjectGetAsync<T>(this IDatabase database, 
            IJsonSerializer serializer, 
            string key,
            bool uncompress = true,
            CommandFlags flags = CommandFlags.None)
        {
            var value = await database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                return default;
            }

            if (!serializer.TryDeserialize(typeof(T), value, uncompress, out var result))
            {
                await database.KeyDeleteAsync(key, flags);
            }

            return (T)result;
        }

        public static bool ObjectSet(this IDatabase database,
            IJsonSerializer serializer,
            string key,
            object value,
            bool compress = true,
            TimeSpan? expiry = null,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            Guard.NotEmpty(key, nameof(key));

            if (serializer.CanDeserialize(value?.GetType()) && serializer.TrySerialize(value, compress, out var buffer))
            {
                return database.StringSet(key, buffer, expiry, when, flags);
            }

            return false;
        }

        public static Task<bool> ObjectSetAsync(this IDatabase database,
            IJsonSerializer serializer,
            string key,
            object value,
            bool compress = true,
            TimeSpan? expiry = null,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            Guard.NotEmpty(key, nameof(key));

            if (serializer.CanDeserialize(value?.GetType()) && serializer.TrySerialize(value, compress, out var buffer))
            {
                return database.StringSetAsync(key, buffer, expiry, when, flags);
            }

            return Task.FromResult(false);
        }
    }
}
