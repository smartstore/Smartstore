using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Smartstore.Core.Web
{
    public class DefaultUserAgent : IUserAgent2
    {
        const int UaStringSizeLimit = 512;

        private static readonly IMemoryCache _memCache = new MemoryCache(new MemoryCacheOptions 
        {
            SizeLimit = 2048
        });
        
        private readonly IUserAgentParser _parser;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private string _userAgent;
        private UserAgentInformation _info;
        private SemanticVersion _version;
        private bool _versionParsed;
        private bool? _supportsWebP;

        public DefaultUserAgent(/*IUserAgentParser parser, */IHttpContextAccessor httpContextAccessor)
        {
            _parser = new DefaultUserAgentParser();
            _httpContextAccessor = httpContextAccessor;
            _userAgent = _httpContextAccessor.HttpContext?.Request?.UserAgent() ?? string.Empty;
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
                _supportsWebP = null;
            }
        }

        public virtual UserAgentType Type
        {
            get => _info.Type;
        }

        public virtual string Name
        {
            get => _info.Name ?? UserAgentInformation.Unknown;
        }

        public virtual SemanticVersion Version
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

                    _ = SemanticVersion.TryParse(_info.Version, out _version);
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

        public virtual bool SupportsWebP
        {
            get 
            {
                if (_supportsWebP == null)
                {
                    if (Version == null)
                    {
                        _supportsWebP = false;
                    }
                    else
                    {
                        var name = Name;
                        var v = Version.Version;
                        var m = this.IsMobileDevice();

                        if (name == "Chrome")
                        {
                            _supportsWebP = v.Major >= (m ? 79 : 32);
                        }
                        else if (name == "Firefox")
                        {
                            _supportsWebP = v.Major >= (m ? 68 : 65);
                        }
                        else if (name == "Edge")
                        {
                            _supportsWebP = v.Major >= 18;
                        }
                        else if (name == "Opera")
                        {
                            _supportsWebP = m || v.Major >= 19;
                        }
                        else if (name == "Safari")
                        {
                            _supportsWebP = v.Major >= (m ? 14 : 16);
                        }
                        else
                        {
                            _supportsWebP = false;
                        }
                    }
                }

                return _supportsWebP.Value;
            }
        }

        protected virtual UserAgentInformation GetUserAgentInfo(string userAgent)
        {
            Guard.NotNull(userAgent);

            if (userAgent.Length > UaStringSizeLimit)
            {
                // Limiting the length of the useragent string protects from hackers sending in extremely long user agent strings.
                userAgent = userAgent[..UaStringSizeLimit];
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
