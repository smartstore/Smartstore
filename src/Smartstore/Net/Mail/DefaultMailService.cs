using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.IO;
using MimeKit.Text;

namespace Smartstore.Net.Mail
{
    public partial class DefaultMailService : IMailService
    {
        public virtual ISmtpClient Connect(IMailAccount account, int timeout = 1000)
        {
            Guard.NotNull(account, nameof(account));

            var mClient = new SmtpClient
            {
                ServerCertificateValidationCallback = ValidateServerCertificate,
                Timeout = timeout
            };

            var client = new MailKitSmtpClient(mClient, account);
            client.Connect();

            return client;
        }

        public virtual async Task<ISmtpClient> ConnectAsync(IMailAccount account, int timeout = 1000)
        {
            Guard.NotNull(account, nameof(account));

            var mClient = new SmtpClient
            {
                ServerCertificateValidationCallback = ValidateServerCertificate,
                Timeout = timeout
            };

            var client = new MailKitSmtpClient(mClient, account);
            await client.ConnectAsync();

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

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // TODO: (core) Make ValidateServerCertificate overridable later. 
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
            var multipart = new Multipart();

            if (original.AltText.HasValue())
            {
                multipart.Add(new TextPart(TextFormat.Html) { Text = original.Body });
                multipart.Add(new TextPart(TextFormat.Text) { Text = original.AltText });
            }
            else
            {
                var textFormat = original.BodyFormat == MailBodyFormat.Html ? TextFormat.Html : TextFormat.Text;
                multipart.Add(new TextPart(textFormat) { Text = original.Body });
            }

            // Attachments
            foreach (var attachment in original.Attachments)
            {
                multipart.Add(BuildMimePart(attachment));
            }

            msg.Body = multipart;

            // Headers
            foreach (var kvp in original.Headers)
            {
                msg.Headers.Add(kvp.Key, kvp.Value);
            }

            return msg;
        }

        /// <summary>
        /// Builds <see cref="MimePart"/> from <see cref="MailAttachment"/>
        /// </summary>
        /// <param name="original">The generic mail attachment</param>
        /// <returns><see cref="MimePart"/> instance</returns>  
        private static MimePart BuildMimePart(MailAttachment original)
        {
            Guard.NotNull(original, nameof(original));

            if (!ContentType.TryParse(original.ContentType, out var mimeContentType))
            {
                mimeContentType = new ContentType("application", "octet-stream");
            }

            return new MimePart(mimeContentType)
            {
                FileName = original.Name,
                Content = new MimeContent(original.ContentStream, (ContentEncoding)original.TransferEncoding),
                ContentDisposition = new ContentDisposition
                {
                    CreationDate = original.CreationDate,
                    ModificationDate = original.ModificationDate,
                    ReadDate = original.ReadDate
                }
            };
        }

        #endregion
    }
}