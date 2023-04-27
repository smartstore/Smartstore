#nullable enable

namespace Smartstore.Core.Web
{
    /// <summary>
    /// Response for parsing and materializing user agent string.
    /// </summary>
    public interface IUserAgentParser
    {
        UserAgentInformation Parse(string? userAgent);
    }
}
