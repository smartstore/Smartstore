using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace Smartstore.Net.Mail
{
    public class SmtpContext
    {
        public SmtpContext(string host, int port = 25)
        {
            Guard.NotEmpty(host, nameof(host));
            Guard.IsPositive(port, nameof(port));

            Host = host;
            Port = port;
        }

        public SmtpContext(IMailAccount account)
        {
            Guard.NotNull(account, nameof(account));

            Host = account.Host;
            Port = account.Port;
            EnableSsl = account.EnableSsl;
            Password = account.Password;
            UseDefaultCredentials = account.UseDefaultCredentials;
            Username = account.Username;
        }

        public bool UseDefaultCredentials
        {
            get;
            set;
        }

        public string Host
        {
            get;
            set;
        }

        public int Port
        {
            get;
            set;
        }

        public string Username
        {
            get;
            set;
        }

        public string Password
        {
            get;
            set;
        }

        public bool EnableSsl
        {
            get;
            set;
        }

        public SmtpClient Connect()
        {
            var client = new SmtpClient
            {
                ServerCertificateValidationCallback = ValidateServerCertificate,
                Timeout = 1000
            };

            try
            {
                client.Connect(
                    Host,
                    Port,
                    EnableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable);

                if (UseDefaultCredentials)
                {
                    client.Authenticate(CredentialCache.DefaultNetworkCredentials);
                }
                else if (Username.HasValue())
                {
                    client.Authenticate(new NetworkCredential(Username, Password));
                }

                return client;
            }
            catch (Exception ex)
            {
                client.Dispose();
                throw new EmailException(ex.Message, ex);
            }
        }

        public async Task<SmtpClient> ConnectAsync()
        {
            var client = new SmtpClient 
            {
                ServerCertificateValidationCallback = ValidateServerCertificate,
                Timeout = 1000
            };

            try
            {
                await client.ConnectAsync(
                    Host,
                    Port,
                    EnableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable);

                if (UseDefaultCredentials)
                {
                    await client.AuthenticateAsync(CredentialCache.DefaultNetworkCredentials);
                }
                else if (Username.HasValue())
                {
                    await client.AuthenticateAsync(new NetworkCredential(Username, Password));
                }

                return client;
            }
            catch (Exception ex)
            {
                client.Dispose();
                throw new EmailException(ex.Message, ex);
            }
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // TODO: (core) Make ValidateServerCertificate overridable later. 
            return true;
        }
    }
}
