namespace Smartstore.Core.Identity
{
    public enum PasswordFormat
    {
        Clear = 0,
        Hashed = 1,
        Encrypted = 2
    }

    /// <summary>
    /// Represents the SameSiteMode for cookies.
    /// </summary>
    public enum SameSiteType
    {
        /// <summary>
        /// No SameSite header should be included on requests.
        /// </summary>
        Unspecified = -1,

        /// <summary>
        /// No mode specified. This setting must be used if the shop runs also in an iframe (SSL must be configured correctly for this).
        /// </summary>
        None = 0,

        /// <summary>
        /// Cookies are sent with requests on the same website and with cross-site navigation at the highest level.
        /// </summary>
        Lax = 1,

        /// <summary>
        /// If the value is "Strict" or invalid, cookies will only be sent to requests on the same website.
        /// </summary>
        Strict = 2
    }
}
