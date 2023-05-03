#nullable enable

namespace Smartstore.Core.Web
{
    /// <summary>
    /// Responsible for parsing and materializing a user agent string.
    /// </summary>
    public interface IUserAgentParser
    {
        UserAgentInfo Parse(string? userAgent);
    }
}
