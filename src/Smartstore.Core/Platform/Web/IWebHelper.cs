#nullable enable

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace Smartstore.Core.Web
{
    public partial interface IWebHelper
    {
        HttpContext? HttpContext { get; }

        /// <summary>
        /// Gets URL referrer or <c>null</c> if Uri parsing fails.
        /// </summary>
        /// <returns>URL referrer</returns>
        Uri? GetUrlReferrer();

        /// <summary>
        /// Gets a unique client identifier
        /// </summary>
        /// <returns>A unique identifier</returns>
        /// <remarks>
        /// The client identifier is a hashed combination of client ip address and user agent.
        /// This method returns <c>null</c> if IP or user agent (or both) cannot be determined.
        /// </remarks>
        string? GetClientIdent();

        /// <summary>
        /// Gets client IP address.
        /// </summary>
        IPAddress GetClientIpAddress();

        /// <summary>
        /// Gets the public IP address of the machine that is hosting the application.
        /// </summary>
        Task<IPAddress> GetPublicIPAddressAsync();

        /// <summary>
        /// Gets the full URL of the current page (including scheme and host part)
        /// </summary>
        /// <param name="withQueryString">Value indicating whether to include query string part</param>
        /// <param name="lowercaseUrl">Value indicating whether to lowercase URL</param>
        /// <returns>Page URL</returns>
        string GetCurrentPageUrl(bool withQueryString = false, bool lowercaseUrl = false);

        /// <summary>
        /// Gets a value indicating whether current connection is secured
        /// </summary>
        /// <returns>true - secured, false - not secured</returns>
        bool IsCurrentConnectionSecured();

        /// <summary>
        /// Gets HTTP header by name
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>HTTP header value</returns>
        string GetHttpHeader(string name);

        /// <summary>
        /// Gets store location (Scheme + Host + PathBase)
        /// </summary>
        /// <returns>Store location</returns>
        string GetStoreLocation();

        /// <summary>
        /// Returns true if the requested resource is one of the typical resources that don't need to be processed by the routing system.
        /// </summary>
        /// <returns>True if the request targets a static resource file.</returns>
        /// <remarks>
        /// All known extensions provided by <see cref="FileExtensionContentTypeProvider"/> are considered to be static resources.
        /// </remarks>
        bool IsStaticFileRequested();

        /// <summary>
        /// Maps a virtual path to a physical disk path.
        /// </summary>
        /// <param name="path">The path to map. E.g. "bin"</param>
        /// <returns>The physical path. E.g. "c:\inetpub\wwwroot\bin"</returns>
        string MapPath(string path);


        /// <summary>
        /// Modifies a URL by merging a query string part and optionally removing a query parameter.
        /// </summary>
        /// <param name="url">URL to modify. Can be relative or absolute. May contain the query part. If <c>null</c>, the current page's URL is resolved (PathBase + Path + Query).</param>
        /// <param name="queryModification">The new query string part (e.g. "page=10") to merge. Leading <c>?</c> char is optional.</param>
        /// <param name="removeParamName">The name of a query param to remove.</param>
        /// <param name="anchor">Optional anchor part to append. Pass without leading hash char.</param>
        /// <returns>Modified url</returns>
        string ModifyQueryString(string? url, string? queryModification, string? removeParamName = null, string? anchor = null);

        /// <summary>
        /// Remove query string from url
        /// </summary>
        /// <param name="uri">Url to modify</param>
        /// <param name="queryParam">Query param to remove</param>
        /// <returns>New url</returns>
        [Obsolete("Call 'ModifyQueryString()' and pass 'removeParamName' parameter.")]
        string RemoveQueryParam(string? uri, string? queryParam);

        /// <summary>
        /// Gets query string value by name
        /// </summary>
        /// <param name="name">Parameter name</param>
        /// <returns>Query string value</returns>
        T? QueryString<T>(string name);
    }
}
