namespace Smartstore.Net.Mail
{
    /// <summary>
    /// Mail/SMTP account abstraction
    /// </summary>
    public interface IMailAccount
    {
        /// <summary>
        /// Gets an email host.
        /// </summary>
        string Host { get; }

        /// <summary>
        /// Gets an email port
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Gets an email user name.
        /// </summary>
        string Username { get; }

        /// <summary>
        /// Gets an email password.
        /// </summary>
        string Password { get; }

        /// <summary>
        /// Gets an option for SSL and/or TLS encryption to be used.
        /// </summary>
        public MailSecureOption MailSecureOption { get; }

        /// <summary>
        /// Gets a value that controls whether the default system credentials of the application are sent with requests.
        /// </summary>
        bool UseDefaultCredentials { get; }
    }
}