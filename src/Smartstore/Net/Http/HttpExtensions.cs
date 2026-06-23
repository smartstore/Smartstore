using System.Net;

namespace Smartstore.Net.Http;

public static class HttpExtensions
{
    /// <summary>
    /// Returns <c>true</c> if the status code is a redirect (301, 302, 303, 307, 308).
    /// </summary>
    public static bool IsRedirect(this HttpStatusCode code)
        => code is HttpStatusCode.MovedPermanently   // 301
                or HttpStatusCode.Found              // 302
                or HttpStatusCode.SeeOther           // 303
                or HttpStatusCode.TemporaryRedirect  // 307
                or HttpStatusCode.PermanentRedirect; // 308
}
