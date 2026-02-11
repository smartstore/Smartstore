#nullable enable

namespace Smartstore.Core.Web;

public class DefaultUserAgent : IUserAgent
{
    private readonly string _userAgent;
    private UserAgentInfo? _info;
    private Func<string, UserAgentInfo>? _infoFactory;
    private Version? _version;
    private bool _versionParsed;

    public DefaultUserAgent(string userAgent, UserAgentInfo info)
    {
        _userAgent = userAgent;
        _info = info;
    }

    public DefaultUserAgent(string userAgent, Func<string, UserAgentInfo> infoFactory)
    {
        _userAgent = userAgent;
        _infoFactory = infoFactory;
    }

    protected internal UserAgentInfo Info
    {
        get
        {
            if (_info == null && _infoFactory != null)
            {
                _info = _infoFactory(_userAgent);
                _infoFactory = null;
            }

            return (UserAgentInfo)_info!;
        }
    }

    public string UserAgent
    {
        get => _userAgent;
    }

    public virtual UserAgentType Type
    {
        get => Info.Type;
    }

    public virtual string Name
    {
        get => Info.Name ?? UserAgentInfo.Unknown;
    }

    public virtual Version? Version
    {
        get
        {
            if (!_versionParsed)
            {
                _versionParsed = true;

                var info = Info;
                if (info.Version.IsEmpty())
                {
                    return null;
                }

                if (SemanticVersion.TryParse(info.Version, out var version))
                {
                    _version = version.Version;
                }
            }
            
            return _version;
        }
    }

    public virtual UserAgentPlatform Platform
    {
        get => Info.Platform;
    }

    public virtual UserAgentDevice Device
    {
        get => Info.Device;
    }
}
