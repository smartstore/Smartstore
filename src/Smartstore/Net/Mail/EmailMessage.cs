using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MimeKit;

namespace Smartstore.Net.Mail
{
    public enum MailBodyFormat
    {
        Text,
        Html
    }

    /// <summary>
    /// Represent an email message.
    /// Named this way to prevent naming conflict with <see cref="System.Net.Mail.MailMessage"/>
    /// </summary>
    public class EmailMessage : ICloneable<EmailMessage>
    {
        public EmailMessage()
        {
        }

        public EmailMessage(string to, string subject, string body, string from)
        {
            Guard.NotEmpty(to, nameof(to));
            Guard.NotEmpty(from, nameof(from));
            Guard.NotEmpty(subject, nameof(subject));
            Guard.NotEmpty(body, nameof(body));

            To.Add(new EmailAddress(to));
            Subject = subject;
            Body = body;
            From = new EmailAddress(from);
        }

        public EmailMessage(EmailAddress to, string subject, string body, EmailAddress from)
            : this()
        {
            Guard.NotNull(to, nameof(to));
            Guard.NotNull(from, nameof(from));
            Guard.NotEmpty(subject, nameof(subject));
            Guard.NotEmpty(body, nameof(body));

            To.Add(to);
            Subject = subject;
            Body = body;
            From = from;
        }

        public EmailAddress From { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string AltText { get; set; }

        public MailBodyFormat BodyFormat { get; set; } = MailBodyFormat.Html;
        public MessagePriority Priority { get; set; } = MessagePriority.Normal;

        public ICollection<EmailAddress> To { get; } = new List<EmailAddress>();
        public ICollection<EmailAddress> Cc { get; } = new List<EmailAddress>();
        public ICollection<EmailAddress> Bcc { get; } = new List<EmailAddress>();
        public ICollection<EmailAddress> ReplyTo { get; } = new List<EmailAddress>();
        public ICollection<MimePart> Attachments { get; } = new List<MimePart>();
        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();

        public async Task BodyFromFile(string filePathOrUrl)
        {
            Guard.NotEmpty(filePathOrUrl, nameof(filePathOrUrl));
            
            StreamReader sr;

            if (filePathOrUrl.ToLower().StartsWith("http"))
            {
                var wc = new WebClient();
                sr = new StreamReader(await wc.OpenReadTaskAsync(filePathOrUrl));
            }
            else
            {
                sr = new StreamReader(filePathOrUrl, Encoding.Default);
            }

            this.Body = await sr.ReadToEndAsync();

            sr.Close();
        }

        #region ICloneable Members

        public EmailMessage Clone()
        {
            var clone = new EmailMessage();

            clone.Attachments.AddRange(this.Attachments);
            clone.To.AddRange(this.To);
            clone.Cc.AddRange(this.Cc);
            clone.Bcc.AddRange(this.Bcc);
            clone.ReplyTo.AddRange(this.ReplyTo);
            clone.Headers.AddRange(this.Headers);

            clone.AltText = this.AltText;
            clone.Body = this.Body;
            clone.BodyFormat = this.BodyFormat;
            clone.From = this.From;
            clone.Priority = this.Priority;
            clone.Subject = this.Subject;

            return clone;
        }

        object ICloneable.Clone()
            => Clone();

        #endregion
    }
}