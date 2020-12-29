using System.Threading.Tasks;
using Smartstore.Core.Messages;

namespace Smartstore.Services.Messages
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
