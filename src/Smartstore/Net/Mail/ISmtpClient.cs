using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
}