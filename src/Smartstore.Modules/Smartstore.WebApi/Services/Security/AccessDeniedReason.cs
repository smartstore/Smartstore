namespace Smartstore.Web.Api.Security
{   
    /// <summary>
    /// Represents the reason for denying an API request.
    /// </summary>
    public enum AccessDeniedReason
    {
        /// <summary>
        /// The API is disabled.
        /// </summary>
        ApiDisabled,

        /// <summary>
        /// HTTPS is required in any case unless the request takes place in a development environment.
        /// </summary>
        SslRequired,

        /// <summary>
        /// The HTTP authorization header is missing or invalid.
        /// Must have the format 'PublicKey:SecretKey'.
        /// </summary>
        InvalidAuthorizationHeader,

        /// <summary>
        /// The credentials sent by the HTTP authorization header do not match those of the user.
        /// </summary>
        InvalidCredentials,

        /// <summary>
        /// The user is unknown.
        /// </summary>
        UserUnknown,

        /// <summary>
        /// The user is known but his access via the API is disabled.
        /// </summary>
        UserDisabled
    }
}
