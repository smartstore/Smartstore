using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace Smartstore.Net.Mail
{
    public partial class MailKitSmtpClient : Disposable, ISmtpClient
    {
        private readonly SmtpClient _client;

        public MailKitSmtpClient(SmtpClient client, IMailAccount account)
        {
            Guard.NotNull(client, nameof(client));
            Guard.NotNull(account, nameof(account));

            _client = client;
            Account = account;
        }

        public IMailAccount Account { get; init; }

        internal void Connect()
        {
            try
            {
                _client.Connect(
                    Account.Host,
                    Account.Port,
                    Account.EnableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable);

                if (Account.UseDefaultCredentials)
                {
                    _client.Authenticate(CredentialCache.DefaultNetworkCredentials);
                }
                else if (Account.Username.HasValue())
                {
                    _client.Authenticate(new NetworkCredential(Account.Username, Account.Password));
                }
            }
            catch (Exception ex)
            {
                _client.Dispose();
                throw new MailException(ex.Message, ex);
            }
        }

        internal async Task ConnectAsync()
        {
            try
            {
                await _client.ConnectAsync(
                    Account.Host,
                    Account.Port,
                    Account.EnableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable);

                if (Account.UseDefaultCredentials)
                {
                    await _client.AuthenticateAsync(CredentialCache.DefaultNetworkCredentials);
                }
                else if (Account.Username.HasValue())
                {
                    await _client.AuthenticateAsync(new NetworkCredential(Account.Username, Account.Password));
                }
            }
            catch (Exception ex)
            {
                _client.Dispose();
                throw new MailException(ex.Message, ex);
            }
        }

        public virtual void Send(IEnumerable<MailMessage> messages, CancellationToken cancelToken = default)
        {
            Guard.NotNull(messages, nameof(messages));

            CheckDisposed();

            foreach (var mimeMessage in messages.Select(original => DefaultMailService.BuildMimeMessage(original)))
            {
                if (cancelToken.IsCancellationRequested)
                    break;

                _client.Send(mimeMessage, cancelToken);
            }
        }

        public virtual async Task SendAsync(IEnumerable<MailMessage> messages, CancellationToken cancelToken = default)
        {
            Guard.NotNull(messages, nameof(messages));

            CheckDisposed();

            foreach (var mimeMessage in messages.Select(original => DefaultMailService.BuildMimeMessage(original)))
            {
                if (cancelToken.IsCancellationRequested)
                    break;

                await _client.SendAsync(mimeMessage, cancelToken);
            }
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                if (_client.IsConnected)
                {
                    _client.Disconnect(true);
                }
                _client.Dispose();
            }
        }

        protected override async ValueTask OnDisposeAsync(bool disposing)
        {
            if (disposing)
            {
                if (_client.IsConnected)
                {
                    await _client.DisconnectAsync(true);
                }
                _client.Dispose();
            }
        }
    }
}