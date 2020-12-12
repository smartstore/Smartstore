using MimeKit;

namespace Smartstore.Net.Mail
{
    /// <summary>
    /// Represent an email address. Just a wrapper for <see cref="MailboxAddress"/>.
    /// Named this way to prevent naming conflict with <see cref="System.Net.Mail.MailAddress"/>
    /// </summary>
    public class EmailAddress
    {
        private readonly MailboxAddress _inner;

        public EmailAddress(string address)
        {
            _inner = MailboxAddress.Parse(address);
        }

        public EmailAddress(string address, string displayName)
        {
            _inner = new MailboxAddress(displayName, address);
        }

        public EmailAddress(MailboxAddress address)
        {
            _inner = address;
        }

        public string Address => _inner.Address;

        public string DisplayName => _inner.Name;

        public override int GetHashCode()
        {
            return _inner.GetHashCode();
        }

        public override string ToString()
        {
            return _inner.ToString();
        }

        public MailboxAddress AsMailBoxAddress()
        {
            return _inner;
        }

        public static implicit operator string(EmailAddress obj)
        {
            return obj.ToString();
        }

        public static implicit operator MailboxAddress(EmailAddress obj)
        {
            return obj.AsMailBoxAddress();
        }
    }
}
