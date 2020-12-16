using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
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
        public MailAddress(string address)
        {
            Guard.NotEmpty(address, nameof(address));

            Address = address;
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

            Address = address;
            DisplayName = displayName;
            DisplayNameEncoding = displayNameEncoding;
        }

        public string Address { get; init; }
        public string DisplayName { get; init; }
        public Encoding DisplayNameEncoding { get; init; }

        /// <summary>
        /// Returns the full address with quoted display name.
        /// i.e. "some email address display name" &lt;user@host&gt;
        /// if displayname is not provided then this returns only user@host (no angle brackets)
        /// </summary>
        public override string ToString()
        {
            if (string.IsNullOrEmpty(DisplayName))
            {
                return Address;
            }
            else
            {
                return "\"" + DisplayName + "\" <" + Address + ">";
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            return ToString().Equals(obj.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}