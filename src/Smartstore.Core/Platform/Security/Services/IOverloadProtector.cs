namespace Smartstore.Core.Security
{
    public interface IOverloadProtector
    {
        Task<bool> DenyGuestAsync();
        Task<bool> DenyCustomerAsync();
        Task<bool> DenyBotAsync();
        Task<bool> ForbidNewGuestAsync();
    }
}
