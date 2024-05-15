using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using MimeKit;
using Smartstore.ComponentModel.TypeConverters;

namespace Smartstore.Net.Mail
{
    /// <summary>
    /// Represent an email address.
    /// </summary>
    [DebuggerDisplay("{Address}")]
    [TypeConverter(typeof(MailAddressConverter))]
    public class MailAddress
    {
        private readonly MailboxAddress _inner;

        public MailAddress(string address)
        {
            Guard.NotEmpty(address);

            // INFO: "TryParse" removes a single quote character, while "Parse" generates a ParseException.
            if (!MailboxAddress.TryParse(address, out _inner) || _inner == null)
            {
                throw new FormatException("Invalid mailbox address: " + address);
            }
        }

        public MailAddress(string address, string displayName)
            : this(address, displayName, Encoding.UTF8)
        {
        }

        public MailAddress(string address, string displayName, Encoding displayNameEncoding)
        {
            Guard.NotEmpty(address, nameof(address));
            Guard.NotEmpty(displayName, nameof(displayName));
            Guard.NotNull(displayNameEncoding, nameof(displayNameEncoding));

            _inner = new MailboxAddress(displayNameEncoding, displayName, address);
        }

        public string Address
        {
            get => _inner.Address;
        }

        public string DisplayName
        {
            get => _inner.Name;
        }

        public Encoding DisplayNameEncoding
        {
            get => _inner.Encoding;
        }

        /// <summary>
        /// Returns the full address with quoted display name.
        /// i.e. "some email address display name" &lt;user@host&gt;
        /// if displayname is not provided then this returns only user@host (no angle brackets)
        /// </summary>
        public override string ToString()
            => _inner.ToString();

        public override bool Equals(object obj)
            => _inner.Equals(obj);

        public override int GetHashCode()
            => _inner.GetHashCode();

        internal MailboxAddress AsMailBoxAddress()
            => _inner;

        public static implicit operator string(MailAddress obj)
            => obj.ToString();

        public static implicit operator MailboxAddress(MailAddress obj)
            => obj.AsMailBoxAddress();
    }

    internal class MailAddressConverter : DefaultTypeConverter
    {
        public MailAddressConverter()
            : base(typeof(object))
        {
        }

        public override bool CanConvertFrom(Type type)
            => type == typeof(string);

        public override bool CanConvertTo(Type type)
            => type == typeof(string);

        public override object ConvertFrom(CultureInfo culture, object value)
        {
            if (value is string str && str.HasValue())
            {
                return new MailAddress(str);
            }

            return base.ConvertFrom(culture, value);
        }

        public override object ConvertTo(CultureInfo culture, string format, object value, Type to)
        {
            if (to == typeof(string) && value is MailAddress address)
            {
                return address.ToString();
            }

            return base.ConvertTo(culture, format, value, to);
        }
    }
}