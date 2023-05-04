using Microsoft.Extensions.Caching.Memory;

namespace Smartstore.Core.Web
{
    public class DefaultUserAgent : IUserAgent
    {
        const int UaStringSizeLimit = 512;

        private static readonly IMemoryCache _memCache = new MemoryCache(new MemoryCacheOptions 
        {
            SizeLimit = 2048
        });
        
        private readonly IUserAgentParser _parser;
        private readonly bool _enableCache;

        private string _userAgent;
        private UserAgentInfo _info;
        private Version _version;
        private bool _versionParsed;

        public DefaultUserAgent(string userAgent, bool enableCache, IUserAgentParser parser)
        {
            Guard.NotNull(userAgent);
            Guard.NotNull(parser);

            _userAgent = userAgent.Trim();
            _parser = parser;
            _enableCache = enableCache;
            _info = GetUserAgentInfo(_userAgent);
        }

        public string UserAgent
        {
            get
            {
                return _userAgent;
            }
            // for (unit) test purpose
            set
            {
                _userAgent = value.EmptyNull().Trim();
                _version = null;
                _versionParsed = false;
                _info = GetUserAgentInfo(value);
            }
        }

        public virtual UserAgentType Type
        {
            get => _info.Type;
        }

        public virtual string Name
        {
            get => _info.Name ?? UserAgentInfo.Unknown;
        }

        public virtual Version Version
        {
            get
            {
                if (!_versionParsed)
                {
                    _versionParsed = true;

                    if (_info.Version.IsEmpty())
                    {
                        return null;
                    }

                    if (SemanticVersion.TryParse(_info.Version, out var version))
                    {
                        _version = version.Version;
                    }
                }
                
                return _version;
            }
        }

        public virtual UserAgentPlatform Platform
        {
            get => _info.Platform;
        }

        public virtual UserAgentDevice Device
        {
            get => _info.Device;
        }

        protected virtual UserAgentInfo GetUserAgentInfo(string userAgent)
        {
            Guard.NotNull(userAgent);

            if (userAgent.Length > UaStringSizeLimit)
            {
                // Limiting the length of the useragent string protects from hackers sending in extremely long user agent strings.
                userAgent = userAgent[..UaStringSizeLimit];
            }

            if (!_enableCache)
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
