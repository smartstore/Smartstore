using System.Threading.Tasks;

namespace Smartstore.Core.Messages
{
    public partial interface IEmailAccountService
    {
        /// <summary>
        /// Gets the default email account.
        /// </summary>
        /// <returns>Email account</returns>
        Task<EmailAccount> GetDefaultEmailAccountAsync();
    }
}
