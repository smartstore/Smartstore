using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using StackExchange.Redis;

namespace Smartstore.Redis
{
    public static class RedisConnectionExtensions
    {
        public static long KeyDeleteWithPattern(this IConnectionMultiplexer connection, string pattern)
        {
            var database = connection.GetDatabase();
            var keys = GetKeys(connection, pattern);
            return database.KeyDelete(keys.Select(key => (RedisKey)key).ToArray());
        }

        public static async Task<long> KeyDeleteWithPatternAsync(this IConnectionMultiplexer connection, string pattern, CancellationToken cancelToken = default)
        {
            var database = connection.GetDatabase();
            var keys = GetKeysAsync(connection, pattern);
            return await database.KeyDeleteAsync(await keys.Select(key => (RedisKey)key).ToArrayAsync());
        }

        public static int KeyCount(this IConnectionMultiplexer connection, string pattern)
        {
            return GetKeys(connection, pattern).Count();
        }

        public static IEnumerable<string> GetKeys(this IConnectionMultiplexer connection, string pattern)
        {
            Guard.NotNull(connection, nameof(connection));
            Guard.NotEmpty(pattern, nameof(pattern));

            foreach (var endPoint in connection.GetEndPoints())
            {
                var server = connection.GetServer(endPoint);
                var redisKeys = server.Keys(pattern: pattern, database: connection.GetDatabase().Database);

                foreach (var key in redisKeys)
                {
                    yield return key;
                }
            }
        }

        public static async IAsyncEnumerable<string> GetKeysAsync(this IConnectionMultiplexer connection, string pattern, [EnumeratorCancellation] CancellationToken cancelToken = default)
        {
            Guard.NotNull(connection, nameof(connection));
            Guard.NotEmpty(pattern, nameof(pattern));

            foreach (var endPoint in connection.GetEndPoints())
            {
                var server = connection.GetServer(endPoint);
                var redisKeys = server.KeysAsync(pattern: pattern, database: connection.GetDatabase().Database);

                await foreach (var key in redisKeys)
                {
                    yield return key;
                }
            }
        }
    }
}