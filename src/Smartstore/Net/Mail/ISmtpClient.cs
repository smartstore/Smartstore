namespace Smartstore.Net.Mail
{
    /// <summary>
    /// SMTP client abstraction. Responsible for sending mails.
    /// </summary>
    public partial interface ISmtpClient : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// The mail account (SMTP host) the client is connected to.
        /// </summary>
        IMailAccount Account { get; }

        /// <summary>
        /// Sends emails.
        /// </summary>
        /// <param name="messages">Messages to send.</param>
        void Send(IEnumerable<MailMessage> messages, CancellationToken cancelToken = default);

        /// <summary>
        /// Sends emails.
        /// </summary>
        /// <param name="messages">Messages to send.</param>
        Task SendAsync(IEnumerable<MailMessage> messages, CancellationToken cancelToken = default);
    }

    public static class ISmtpClientExtensions
    {
        /// <summary>
        /// Sends a single email.
        /// </summary>
        /// <param name="message">Message to send.</param>
        public static void Send(this ISmtpClient client, MailMessage message, CancellationToken cancelToken = default)
        {
            Guard.NotNull(message, nameof(message));
            client.Send(new[] { message }, cancelToken);
        }

        /// <summary>
        /// Sends a single email.
        /// </summary>
        /// <param name="message">Message to send.</param>
        public static Task SendAsync(this ISmtpClient client, MailMessage message, CancellationToken cancelToken = default)
        {
            Guard.NotNull(message, nameof(message));
            return client.SendAsync(new[] { message }, cancelToken);
        }
    }
}