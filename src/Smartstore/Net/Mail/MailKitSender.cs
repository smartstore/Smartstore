using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.IO;
using MimeKit.Text;

namespace Smartstore.Net.Mail
{
    public class MailKitSender : IMailSender
    {
        // TODO: (core) Get PickupDirectoryLocation from EmailAccountSettings without moving this class to Smsrtstore.Core.
        private readonly string _pickupDirectoryLocation;

        //public DefaultMailSender(EmailAccountSettings emailAccountSettings)
        //{
        //    _emailAccountSettings = emailAccountSettings;
        //}

        public void SendMail(SmtpClient client, EmailMessage message)
        {
            Guard.NotNull(client, nameof(client));
            Guard.NotNull(message, nameof(message));

            if (_pickupDirectoryLocation.HasValue())
            {
                SaveToPickupDirectory(BuildMimeMessage(message), _pickupDirectoryLocation);
            }
            else
            {
                client.Send(BuildMimeMessage(message));
            }
        }

        public async Task SendMailAsync(SmtpClient client, EmailMessage message)
        {
            Guard.NotNull(client, nameof(client));
            Guard.NotNull(message, nameof(message));

            if (_pickupDirectoryLocation.HasValue())
            {
                SaveToPickupDirectory(BuildMimeMessage(message), _pickupDirectoryLocation);
            }
            else
            {
                await client.SendAsync(BuildMimeMessage(message));
            }
        }

        /// <summary>
        /// Builds <see cref="MimeMessage"/> from <see cref="EmailMessage"/>
        /// </summary>
        /// <param name="original">The generic mail message</param>
        /// <returns><see cref="MimeMessage"/> instance</returns>        
        protected virtual MimeMessage BuildMimeMessage(EmailMessage original)
        {
            var msg = new MimeMessage();

            if (original.Subject.IsEmpty())
            {
                throw new MailSenderException("Required subject is missing!");
            }

            msg.Subject = original.Subject;
            msg.Priority = original.Priority;

            // Addresses
            msg.From.Add(original.From);
            msg.To.AddRange(original.To.Where(x => x.Address.HasValue()).OfType<MailboxAddress>());
            msg.Cc.AddRange(original.Cc.Where(x => x.Address.HasValue()).OfType<MailboxAddress>());
            msg.Bcc.AddRange(original.Bcc.Where(x => x.Address.HasValue()).OfType<MailboxAddress>());
            msg.ReplyTo.AddRange(original.ReplyTo.Where(x => x.Address.HasValue()).OfType<MailboxAddress>());

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
                multipart.Add(attachment);
            }

            // Headers
            foreach (var kvp in original.Headers)
            {
                msg.Headers.Add(kvp.Key, kvp.Value);
            }

            return msg;
        }

        private static void SaveToPickupDirectory(MimeMessage message, string pickupDirectory)
        {
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

                            message.WriteTo(options, filtered);
                            filtered.Flush();
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
    }
}
