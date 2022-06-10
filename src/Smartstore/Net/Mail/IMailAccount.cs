namespace Smartstore.Net.Mail
{
    /// <summary>
    /// Mail/SMTP account abstraction
    /// </summary>
    public interface IMailAccount
    {
        /// <summary>
        /// Gets or sets an email host
        /// </summary>
        string Host { get; }

        /// <summary>
        /// Gets or sets an email port
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Gets or sets an email user name
        /// </summary>
        string Username { get; }

        /// <summary>
        /// Gets or sets an email password
        /// </summary>
        string Password { get; }

        /// <summary>
        /// Gets or sets a value that controls whether the SmtpClient uses Secure Sockets Layer (SSL) to encrypt the connection
        /// </summary>
        bool EnableSsl { get; }

        /// <summary>
        /// Gets or sets a value that controls whether the default system credentials of the application are sent with requests.
        /// </summary>
        bool UseDefaultCredentials { get; }
    }
}