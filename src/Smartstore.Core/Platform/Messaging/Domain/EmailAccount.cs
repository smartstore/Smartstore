using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Smartstore.Data.Caching;
using Smartstore.Net.Mail;

namespace Smartstore.Core.Messaging
{
    // TODO: (mg) (core) remove required attribute at EmailAccount.Username and EmailAccount.Password later (migration required).

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
        [Required, StringLength(255)]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets an email password.
        /// </summary>
        [Required, StringLength(255)]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets a value that controls whether the SmtpClient uses Secure Sockets Layer (SSL) to encrypt the connection.
        /// </summary>
        public bool EnableSsl { get; set; }

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
        {
            return (EmailAccount)this.MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
