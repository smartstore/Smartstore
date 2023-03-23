namespace Smartstore.Net.Mail
{
    /// <summary>
    /// Represents options which SSL and/or TLS encryption to use for a SMTP connection.
    /// See <see cref="MailKit.Security.SecureSocketOptions"/>.
    /// </summary>
    public enum MailSecureOption
    {
        /// <summary>
        /// No SSL or TLS encryption should be used.
        /// </summary>
        None = 0,

        /// <summary>
        /// Allow the IMailService to decide which SSL or TLS options to use (default). If the server does not support SSL or TLS,
        /// then the connection will continue without any encryption.
        /// </summary>
        Auto = 1,

        /// <summary>
        /// The connection should use SSL or TLS encryption immediately.
        /// </summary>
        SslOnConnect = 2,

        /// <summary>
        /// Elevates the connection to use TLS encryption immediately after reading the greeting and capabilities of the server.
        /// If the server does not support the STARTTLS extension, then the connection will fail and a NotSupportedException will be thrown.
        /// </summary>
        StartTls = 3,

        /// <summary>
        /// Elevates the connection to use TLS encryption immediately after reading the greeting and capabilities of the server,
        /// but only if the server supports the STARTTLS extension.
        /// </summary>
        StartTlsWhenAvailable = 4
    }
}
