using Smartstore.Core.Messaging;
using Smartstore.Net.Mail;

namespace Smartstore
{
    public static class EmailAccountExtensions
    {
        /// <summary>
        /// Creates a new <see cref="MailAddress"/> based on <see cref="EmailAccount.Email"/> and <see cref="EmailAccount.DisplayName"/>.
        /// </summary>
        public static MailAddress ToMailAddress(this EmailAccount emailAccount)
        {
            Guard.NotNull(emailAccount, nameof(emailAccount));

            if (!emailAccount.DisplayName.HasValue())
            {
                return new MailAddress(emailAccount.Email);
            }

            return new MailAddress(emailAccount.Email, emailAccount.DisplayName);
        }
    }
}
