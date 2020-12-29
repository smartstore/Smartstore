using Smartstore.Net.Mail;
using Smartstore.Core.Messages;

namespace Smartstore
{
    public static class EmailAccountExtensions
    {
        /// <summary>
        /// Creates a new <see cref="MailAddress"/> based on <see cref="EmailAccount.Email"/> and <see cref="EmailAccount.DisplayName"/>.
        /// </summary>
        public static MailAddress ToEmailAddress(this EmailAccount emailAccount)
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
