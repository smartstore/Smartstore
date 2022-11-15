using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.IO;
using Smartstore.Engine;

namespace Smartstore.Net.Mail
{
    public partial class DefaultMailService : IMailService
    {
        private readonly int _smtpTimeout;
        
        public DefaultMailService(SmartConfiguration appConfig)
        {
            _smtpTimeout = appConfig.SmtpServerTimeout;
        }

        public virtual ISmtpClient Connect(IMailAccount account, int? timeout)
        {
            return ConnectCore(account, timeout, false).Await();
        }

        public virtual Task<ISmtpClient> ConnectAsync(IMailAccount account, int? timeout)
        {
            return ConnectCore(account, timeout, true);
        }

        protected virtual async Task<ISmtpClient> ConnectCore(IMailAccount account, int? timeout, bool async)
        {
            var smtpClient = new SmtpClient
            {
                ServerCertificateValidationCallback = OnValidateServerCertificate,
                Timeout = timeout ?? _smtpTimeout
            };

            var client = new MailKitSmtpClient(smtpClient, account);

            if (async)
            {
                await client.ConnectAsync();
            }
            else
            {
                client.Connect();
            }

            return client;
        }

        public virtual async Task SaveAsync(string pickupDirectory, MailMessage message)
        {
            Guard.NotEmpty(pickupDirectory, nameof(pickupDirectory));
            Guard.NotNull(message, nameof(message));

            var mimeMessage = BuildMimeMessage(message);

            do
            {
                // Generate a random file name to save the message to.
                var path = Path.Combine(pickupDirectory, Guid.NewGuid().ToString() + ".eml");
                Stream stream;

                try
                {
                    // Attempt to create the new file.
                    stream = File.Open(path, FileMode.CreateNew);
                }
                catch (IOException)
                {
                    // If the file already exists, try again with a new Guid.
                    if (File.Exists(path))
                        continue;

                    // Otherwise, fail immediately since it probably means that there is
                    // no graceful way to recover from this error.
                    throw;
                }

                try
                {
                    using (stream)
                    {
                        // IIS pickup directories expect the message to be "byte-stuffed"
                        // which means that lines beginning with "." need to be escaped
                        // by adding an extra "." to the beginning of the line.
                        //
                        // Use an SmtpDataFilter "byte-stuff" the message as it is written
                        // to the file stream. This is the same process that an SmtpClient
                        // would use when sending the message in a `DATA` command.
                        using (var filtered = new FilteredStream(stream))
                        {
                            filtered.Add(new SmtpDataFilter());

                            // Make sure to write the message in DOS (<CR><LF>) format.
                            var options = FormatOptions.Default.Clone();
                            options.NewLineFormat = NewLineFormat.Dos;

                            await mimeMessage.WriteToAsync(options, filtered);
                            await filtered.FlushAsync();
                            return;
                        }
                    }
                }
                catch
                {
                    // An exception here probably means that the disk is full.
                    //
                    // Delete the file that was created above so that incomplete files are not
                    // left behind for IIS to send accidentally.
                    File.Delete(path);
                    throw;
                }
            } while (true);
        }

        protected virtual bool OnValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        #region Converters

        /// <summary>
        /// Builds <see cref="MimeMessage"/> from <see cref="MailMessage"/>
        /// </summary>
        /// <param name="original">The generic mail message</param>
        /// <returns><see cref="MimeMessage"/> instance</returns>        
        protected internal static MimeMessage BuildMimeMessage(MailMessage original)
        {
            Guard.NotNull(original, nameof(original));

            if (original.Subject.IsEmpty())
            {
                throw new MailException("Required subject is missing!");
            }

            if (original.From is null)
            {
                throw new MailException("Required sender is missing!");
            }

            var msg = new MimeMessage
            {
                Subject = original.Subject,
                Priority = (MessagePriority)original.Priority
            };

            // Addresses
            msg.From.Add(original.From.AsMailBoxAddress());
            msg.To.AddRange(original.To.Where(x => x.Address.HasValue()).Select(x => x.AsMailBoxAddress()));
            msg.Cc.AddRange(original.Cc.Where(x => x.Address.HasValue()).Select(x => x.AsMailBoxAddress()));
            msg.Bcc.AddRange(original.Bcc.Where(x => x.Address.HasValue()).Select(x => x.AsMailBoxAddress()));
            msg.ReplyTo.AddRange(original.ReplyTo.Where(x => x.Address.HasValue()).Select(x => x.AsMailBoxAddress()));

            // Body
            var builder = new BodyBuilder();

            // Attachments
            foreach (var attachment in original.Attachments)
            {
                ProcessAttachment(attachment, builder);
            }

            if (original.AltText.HasValue())
            {
                builder.HtmlBody = original.Body;
                builder.TextBody = original.AltText;
            }
            else
            {
                if (original.BodyFormat == MailBodyFormat.Html)
                {
                    builder.HtmlBody = original.Body;
                }
                else
                {
                    builder.TextBody = original.Body;
                }
            }

            msg.Body = builder.ToMessageBody();

            // Headers
            foreach (var kvp in original.Headers)
            {
                msg.Headers.Add(kvp.Key, kvp.Value);
            }

            return msg;
        }

        /// <summary>
        /// Builds <see cref="MimePart"/> from <paramref name="attachment"/> attachments
        /// and adds it to underlying attachments collection.
        /// </summary>
        /// <param name="original">The generic mail attachment</param>
        private static MimeEntity ProcessAttachment(MailAttachment attachment, BodyBuilder builder)
        {
            Guard.NotNull(attachment, nameof(attachment));

            ContentType contentType = null;
            if (attachment.ContentType.HasValue())
            {
                _ = ContentType.TryParse(attachment.ContentType, out contentType);
            }

            var collection = attachment.IsEmbedded ? builder.LinkedResources : builder.Attachments;

            var mimeEntity = contentType == null 
                ? collection.Add(attachment.Name, attachment.ContentStream)
                : collection.Add(attachment.Name, attachment.ContentStream, contentType);

            if (attachment.IsEmbedded)
            {
                mimeEntity.ContentId = attachment.ContentId;
            }

            var disposition = mimeEntity.ContentDisposition;
            if (disposition != null)
            {
                disposition.CreationDate = attachment.CreationDate;
                disposition.ModificationDate = attachment.ModificationDate;
                disposition.ReadDate = attachment.ReadDate;
            }

            return mimeEntity;
        }

        #endregion
    }
}