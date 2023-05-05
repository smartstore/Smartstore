namespace Smartstore.Core.Web
{
    public class DefaultUserAgent : IUserAgent
    {
        private string _userAgent;
        private UserAgentInfo _info;
        private Version _version;
        private bool _versionParsed;

        public DefaultUserAgent(string userAgent, UserAgentInfo info)
        {
            _userAgent = userAgent;
            _info = info;
        }

        public string UserAgent
        {
            get => _userAgent;
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
    }
}
