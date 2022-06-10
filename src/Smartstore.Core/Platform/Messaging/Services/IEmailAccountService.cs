namespace Smartstore.Core.Messaging
{
    public partial interface IEmailAccountService
    {
        /// <summary>
        /// Gets the default email account.
        /// </summary>
        /// <returns>Email account</returns>
        EmailAccount GetDefaultEmailAccount();
    }
}
