using System.Net.Http;
using System.Text;
using Smartstore.Engine;

namespace Smartstore.Net.Mail
{
    public enum MailBodyFormat
    {
        Text,
        Html
    }

    public enum MailPriority
    {
        Normal,
        Low,
        High,
    }

    /// <summary>
    /// Represent an email message.
    /// </summary>
    public class MailMessage : Disposable, ICloneable<MailMessage>
    {
        public MailMessage()
        {
        }

        public MailMessage(string to, string subject, string body, string from)
        {
            Guard.NotEmpty(to, nameof(to));
            Guard.NotEmpty(subject, nameof(subject));
            Guard.NotEmpty(body, nameof(body));
            Guard.NotEmpty(from, nameof(from));

            To.AddRange(TokenizeAddressParameter(to));
            Subject = subject;
            Body = body;
            From = new MailAddress(from);
        }

        public MailMessage(MailAddress to, string subject, string body, MailAddress from)
            : this()
        {
            Guard.NotNull(to, nameof(to));
            Guard.NotEmpty(subject, nameof(subject));
            Guard.NotEmpty(body, nameof(body));
            Guard.NotNull(from, nameof(from));

            To.Add(to);
            Subject = subject;
            Body = body;
            From = from;
        }

        public MailAddress From { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string AltText { get; set; }

        public MailBodyFormat BodyFormat { get; set; } = MailBodyFormat.Html;
        public MailPriority Priority { get; set; } = MailPriority.Normal;

        public ICollection<MailAddress> To { get; } = new List<MailAddress>();
        public ICollection<MailAddress> Cc { get; } = new List<MailAddress>();
        public ICollection<MailAddress> Bcc { get; } = new List<MailAddress>();
        public ICollection<MailAddress> ReplyTo { get; } = new List<MailAddress>();
        public ICollection<MailAttachment> Attachments { get; } = new List<MailAttachment>();
        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();

        private static IEnumerable<MailAddress> TokenizeAddressParameter(string addresses)
        {
            return addresses
                .Trim()
                .SplitSafe(';')
                .Where(x => x.Trim().HasValue())
                .Select(x => new MailAddress(x));
        }

        public async Task BodyFromFileAsync(string filePathOrUrl)
        {
            Guard.NotEmpty(filePathOrUrl, nameof(filePathOrUrl));

            StreamReader sr;

            if (filePathOrUrl.ToLower().StartsWith("http"))
            {
                var httpClientFactory = EngineContext.Current.Scope.ResolveOptional<IHttpClientFactory>();
                var client = httpClientFactory?.CreateClient() ?? new HttpClient();

                sr = new StreamReader(await client.GetStreamAsync(filePathOrUrl));
            }
            else
            {
                sr = new StreamReader(filePathOrUrl, Encoding.Default);
            }

            Body = await sr.ReadToEndAsync();

            sr.Close();
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                Attachments.Each(x => x.Dispose());
                Attachments.Clear();
            }
        }

        #region ICloneable Members

        public MailMessage Clone()
        {
            var clone = new MailMessage();

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