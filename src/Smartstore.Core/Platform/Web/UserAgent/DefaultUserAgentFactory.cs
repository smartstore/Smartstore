using Microsoft.Extensions.Caching.Memory;

namespace Smartstore.Core.Web
{
    public class DefaultUserAgentFactory : IUserAgentFactory
    {
        const int UaStringSizeLimit = 512;

        private readonly IUserAgentParser _parser;
        private readonly IMemoryCache _memCache;

        public DefaultUserAgentFactory(IUserAgentParser parser) 
        {
            _parser = parser;
            _memCache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 2048
            });
        }

        public IUserAgent CreateUserAgent(string userAgent, bool enableCache = true)
        {
            Guard.NotNull(userAgent);

            userAgent = userAgent.Trim();

            var info = GetUserAgentInfo(userAgent, enableCache);
            return new DefaultUserAgent(userAgent, info);
        }

        protected virtual UserAgentInfo GetUserAgentInfo(string userAgent, bool enableCache)
        {
            if (userAgent.Length > UaStringSizeLimit)
            {
                // Limiting the length of the useragent string protects from hackers sending in extremely long user agent strings.
                userAgent = userAgent[..UaStringSizeLimit];
            }

            if (!enableCache)
            {
                return _parser.Parse(userAgent);
            }

            return _memCache.GetOrCreate(userAgent, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromDays(1);
                entry.SetSize(1);

                return _parser.Parse(userAgent);
            });
        }
    }
}
