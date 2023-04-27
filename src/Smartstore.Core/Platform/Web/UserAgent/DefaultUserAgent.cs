using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Smartstore.Core.Web
{
    public class DefaultUserAgent : IUserAgent2
    {
        private static readonly IMemoryCache _memCache = new MemoryCache(new MemoryCacheOptions 
        {
            SizeLimit = 2048
        });
        
        private readonly IUserAgentParser _parser;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private string _rawValue;
        private UserAgentInformation _info;
        private SemanticVersion _version;
        private bool? _isTablet;
        private bool? _isPdfConverter;

        public DefaultUserAgent(/*IUserAgentParser parser, */IHttpContextAccessor httpContextAccessor)
        {
            _parser = new DefaultUserAgentParser();
            _httpContextAccessor = httpContextAccessor;
            _rawValue = _httpContextAccessor.HttpContext?.Request?.UserAgent() ?? string.Empty;
            _info = GetUserAgentInfo(_rawValue);
        }

        public string RawValue
        {
            get
            {
                return _rawValue;
            }
            // for (unit) test purpose
            set
            {
                _rawValue = value.EmptyNull().Trim();
                _version = null;
                _isTablet = null;
                _isPdfConverter = null;
                _info = GetUserAgentInfo(value);
            }
        }

        public virtual bool IsBot
        {
            get => _info.IsBot();
        }

        public virtual bool IsMobileDevice
        {
            get => _info.IsMobile();
        }

        public virtual bool IsTablet
        {
            get => false; // TODO
        }

        public virtual bool IsPdfConverter
        {
            get
            {
                if (!_isPdfConverter.HasValue)
                {
                    _isPdfConverter = RawValue.EqualsNoCase("wkhtmltopdf");
                }

                return _isPdfConverter.Value;
            }
        }

        public virtual string Name
        {
            get => _info.Name;
        }

        public virtual SemanticVersion Version
        {
            get
            {
                if (_version == null)
                {
                    if (_info.Version.IsEmpty() || !SemanticVersion.TryParse(_info.Version, out _version))
                    {
                        _version = new SemanticVersion(0, 0);
                    }
                }
                
                return _version;
            }
        }

        public virtual UserAgentPlatform? Platform
        {
            get => _info.Platform;
        }

        public virtual string MobileDeviceName
        {
            get => _info.MobileDeviceName;
        }

        protected virtual UserAgentInformation GetUserAgentInfo(string userAgent)
        {
            Guard.NotNull(userAgent);

            return _memCache.GetOrCreate(userAgent, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromDays(1);
                entry.SetSize(1);

                return _parser.Parse(userAgent);
            });
        }
    }
}
