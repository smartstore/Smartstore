using Smartstore.Caching;

namespace Smartstore.Core.AI
{
    public class DefaultAIChatCache(ICacheManager cache) : IAIChatCache
    {
        const string CacheKeyPrefix = "aichat:";

        private static readonly TimeSpan ChatCacheDuration = TimeSpan.FromMinutes(20);
        private readonly ICacheManager _cache = cache;

        public string GenerateSessionToken()
            => Guid.NewGuid().ToString("N");

        public Task<AIChat> GetAsync(string token)
        {
            Guard.NotEmpty(token);
            return _cache.GetAsync<AIChat>(BuildCacheKey(token));
        }

        public Task PutAsync(string token, AIChat chat, TimeSpan? slidingExpiration = null)
        {
            Guard.NotEmpty(token);
            Guard.NotNull(chat);
            
            return _cache.PutAsync(
                BuildCacheKey(token), 
                chat, 
                new CacheEntryOptions().SetSlidingExpiration(slidingExpiration ?? ChatCacheDuration));
        }

        public Task RemoveAsync(string token)
        {
            if (token.HasValue())
            {
                return _cache.RemoveAsync(BuildCacheKey(token));
            }

            return Task.CompletedTask;
        }

        private static string BuildCacheKey(string token)
            => CacheKeyPrefix + token;
    }
}
