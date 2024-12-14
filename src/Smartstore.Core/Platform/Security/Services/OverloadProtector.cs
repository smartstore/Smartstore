namespace Smartstore.Core.Security
{
    public class OverloadProtector : IOverloadProtector
    {
        public Task<bool> DenyGuestAsync()
        {
            return Task.FromResult(false);
        }

        public Task<bool> DenyCustomerAsync()
        {
            return Task.FromResult(false);
        }

        public Task<bool> DenyBotAsync()
        {
            return Task.FromResult(false);
        }

        public Task<bool> ForbidNewGuestAsync()
        {
            return Task.FromResult(false);
        }
    }
}
