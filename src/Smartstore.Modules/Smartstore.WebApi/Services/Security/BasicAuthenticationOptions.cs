using Microsoft.AspNetCore.Authentication;
using Smartstore.Utilities;

namespace Smartstore.Web.Api.Security
{
    /// <summary>
    /// Contains the options used by <see cref="BasicAuthenticationHandler"/>.
    /// </summary>
    public class BasicAuthenticationOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the WWW-Authenticate header will be suppressed on unauthorized responses.
        /// </summary>
        /// <remarks>
        /// "WWW-Authenticate" header causes the browser to prompt for credentials.
        /// If it is missing the server just sends 401 status code.
        /// </remarks>
        public bool SuppressWWWAuthenticateHeader { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SSL is required.
        /// <c>False</c> is unsafe and should only be used with caution.
        /// </summary>
        public bool SslRequired { get; set; } = !CommonHelper.IsDevEnvironment;
    }
}
