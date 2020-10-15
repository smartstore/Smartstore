using System.Web;
using Microsoft.AspNetCore.Http;

namespace Smartstore.Web
{
    public partial interface IWebHelper
    {
        /// <summary>
        /// Get URL referrer
        /// </summary>
        /// <returns>URL referrer</returns>
        string GetUrlReferrer();

        /// <summary>
        /// Gets a unique client identifier
        /// </summary>
        /// <returns>A unique identifier</returns>
        /// <remarks>
        /// The client identifier is a hashed combination of client ip address and user agent.
        /// This method returns <c>null</c> if IP or user agent (or both) cannot be determined.
        /// </remarks>
        string GetClientIdent();

        /// <summary>
        /// Get context IP address
        /// </summary>
        /// <returns>URL referrer</returns>
        string GetCurrentIpAddress();

        /// <summary>
        /// Gets the full URL of the current page (including scheme and host part)
        /// </summary>
        /// <param name="withQueryString">Value indicating whether to include query string part</param>
        /// <param name="secured">Value indicating whether to get SSL protected page URL. Pass <c>null</c> to auto-determine scheme.</param>
        /// <param name="lowercaseUrl">Value indicating whether to lowercase URL</param>
        /// <returns>Page URL</returns>
        string GetCurrentPageUrl(bool withQueryString = false, bool? secured = null, bool lowercaseUrl = false);

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
        /// <param name="secured">Value indicating whether to get SSL protected location. Pass <c>null</c> to auto-determine scheme.</param>
        /// <returns>Store location</returns>
        string GetStoreLocation(bool? secured = null);

        /// <summary>
        /// Returns true if the requested resource is one of the typical resources that needn't be processed by the cms engine.
        /// </summary>
        /// <param name="request">HTTP Request</param>
        /// <returns>True if the request targets a static resource file.</returns>
        /// <remarks>
        /// These are the file extensions considered to be static resources:
        /// .css
        ///	.gif
        /// .png 
        /// .jpg
        /// .jpeg
        /// .js
        /// .axd
        /// .ashx
        /// </remarks>
        bool IsStaticResource(HttpRequest request);

        /// <summary>
        /// Maps a virtual path to a physical disk path.
        /// </summary>
        /// <param name="path">The path to map. E.g. "bin"</param>
        /// <returns>The physical path. E.g. "c:\inetpub\wwwroot\bin"</returns>
        string MapPath(string path);


        /// <summary>
        /// Modifies query string
        /// </summary>
        /// <param name="url">Url to modify</param>
        /// <param name="queryStringModification">Query string modification, e.g. "param=value&param2=value2"</param>
        /// <param name="anchor">Add anchor part. Pass without hash char.</param>
        /// <returns>Modified url</returns>
        string ModifyQueryString(string url, string queryStringModification, string anchor = null);

        /// <summary>
        /// Remove query string from url
        /// </summary>
        /// <param name="url">Url to modify</param>
        /// <param name="queryParam">Query param to remove</param>
        /// <returns>New url</returns>
        string RemoveQueryParam(string url, string queryParam);

        /// <summary>
        /// Gets query string value by name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">Parameter name</param>
        /// <returns>Query string value</returns>
        T QueryString<T>(string name);

        /// <summary>
        /// Restart application domain
        /// </summary>
		void RestartAppDomain();
    }
}
