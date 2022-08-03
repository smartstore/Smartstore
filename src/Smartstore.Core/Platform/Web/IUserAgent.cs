namespace Smartstore.Core.Web
{
    public interface IUserAgent
    {
        string RawValue { get; set; }

        UserAgentInfo UserAgent { get; }
        DeviceInfo Device { get; }
        OSInfo OS { get; }

        bool IsBot { get; }
        bool IsMobileDevice { get; }
        bool IsTablet { get; }
        bool IsPdfConverter { get; }
    }

    public sealed class DeviceInfo
    {
        public DeviceInfo(string family, bool isBot)
        {
            Family = family;
            IsBot = isBot;
        }

        public string Family { get; private set; }
        public bool IsBot { get; private set; }

        public override string ToString() => Family;
    }

    public sealed class OSInfo
    {
        public OSInfo(string family, string major, string minor, string patch, string patchMinor)
        {
            Family = family;
            Major = major;
            Minor = minor;
            Patch = patch;
            PatchMinor = patchMinor;
        }

        public string Family { get; private set; }
        public string Major { get; private set; }
        public string Minor { get; private set; }
        public string Patch { get; private set; }
        public string PatchMinor { get; private set; }

        public override string ToString()
        {
            var str = VersionString.Format(Major, Minor, Patch, PatchMinor);
            return (Family + (!string.IsNullOrEmpty(str) ? (" " + str) : null));
        }
    }

    public sealed class UserAgentInfo
    {
        private static readonly HashSet<string> s_Bots = new(StringComparer.InvariantCultureIgnoreCase)
        {
            "BingPreview"
        };

        private bool? _isBot;
        private bool? _supportsWebP;

        public UserAgentInfo(string family, string major, string minor, string patch)
        {
            Family = family;
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public string Family { get; private set; }
        public string Major { get; private set; }
        public string Minor { get; private set; }
        public string Patch { get; private set; }

        public bool IsBot
        {
            get
            {
                if (!_isBot.HasValue)
                {
                    _isBot = s_Bots.Contains(Family);
                }
                return _isBot.Value;
            }
        }

        public bool SupportsWebP
        {
            get
            {
                if (_supportsWebP == null)
                {
                    if (Family == "Chrome")
                    {
                        _supportsWebP = Major.ToInt() >= 32;
                    }
                    else if (Family.StartsWith("Chrome Mobile"))
                    {
                        _supportsWebP = Major.ToInt() >= 79;
                    }
                    else if (Family == "Firefox")
                    {
                        _supportsWebP = Major.ToInt() >= 65;
                    }
                    else if (Family.StartsWith("Firefox Mobile"))
                    {
                        _supportsWebP = Major.ToInt() >= 68;
                    }
                    else if (Family == "Edge")
                    {
                        _supportsWebP = Major.ToInt() >= 18;
                    }
                    else if (Family == "Opera")
                    {
                        _supportsWebP = Major.ToInt() >= 19;
                    }
                    else if (Family == "Opera Mini")
                    {
                        _supportsWebP = true;
                    }
                    else if (Family.StartsWith("Android"))
                    {
                        _supportsWebP = Major.ToInt() >= 5 || (Major.ToInt() == 4 && Minor.ToInt() >= 2);
                    }
                    else if (Family.StartsWith("Samsung Internet"))
                    {
                        _supportsWebP = Major.ToInt() >= 4;
                    }
                    else if (Family.StartsWith("Baidu"))
                    {
                        _supportsWebP = Major.ToInt() >= 8 || (Major.ToInt() == 7 && Minor.ToInt() >= 12);
                    }
                    else
                    {
                        _supportsWebP = false;
                    }
                }

                return _supportsWebP.Value;
            }
        }

        public override string ToString()
        {
            var str = VersionString.Format(Major, Minor, Patch);
            return (Family + (!string.IsNullOrEmpty(str) ? (" " + str) : null));
        }
    }

    internal static class VersionString
    {
        public static string Format(params string[] parts)
        {
            return string.Join(".", (from v in parts
                                     where !string.IsNullOrEmpty(v)
                                     select v).ToArray<string>());
        }
    }
}
