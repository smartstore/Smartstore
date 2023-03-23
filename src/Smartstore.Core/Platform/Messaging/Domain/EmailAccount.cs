using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Smartstore.Data.Caching;
using Smartstore.Net.Mail;

namespace Smartstore.Core.Messaging
{
    /// <summary>
    /// Represents an email account.
    /// </summary>
    [CacheableEntity]
    public partial class EmailAccount : EntityWithAttributes, ICloneable<EmailAccount>, IMailAccount
    {
        /// <summary>
        /// Gets or sets an email address.
        /// </summary>
        [Required, StringLength(255)]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets an email display name.
        /// </summary>
        [StringLength(255)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets an email host.
        /// </summary>
        [Required, StringLength(255)]
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets an email port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets an email user name.
        /// </summary>
        [StringLength(255)]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets an email password.
        /// </summary>
        [StringLength(255)]
        public string Password { get; set; }

        [Obsolete("Use SecureOption instead.")]
        public bool EnableSsl { get; set; }

        /// <summary>
        /// Gets or sets the option value for SSL and/or TLS encryption to use.
        /// </summary>
        public int SecureOption { get; set; }

        /// <summary>
        /// Gets or sets an option for SSL and/or TLS encryption to use.
        /// </summary>
        [NotMapped]
        public MailSecureOption MailSecureOption
        {
            get => (MailSecureOption)SecureOption;
            set => SecureOption = (int)value;
        }

        /// <summary>
        /// Gets or sets a value that controls whether the default system credentials of the application are sent with requests.
        /// </summary>
        public bool UseDefaultCredentials { get; set; }

        /// <summary>
        /// Gets a friendly email account name.
        /// </summary>
        [NotMapped, IgnoreDataMember]
        public string FriendlyName
        {
            get
            {
                if (DisplayName.IsEmpty())
                    return Email;

                return $"{DisplayName} ({Email})";
            }
        }

        public EmailAccount Clone()
            => (EmailAccount)MemberwiseClone();

        object ICloneable.Clone()
            => MemberwiseClone();
    }
}
