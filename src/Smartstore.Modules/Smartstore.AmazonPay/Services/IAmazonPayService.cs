using Amazon.Pay.API.WebStore.CheckoutSession;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;

namespace Smartstore.AmazonPay.Services
{
    public interface IAmazonPayService
    {
        Task<bool> AddCustomerOrderNoteLoopAsync(AmazonPayActionState state, CancellationToken cancelToken = default);

        bool HasCheckoutState();
        AmazonPayCheckoutState GetCheckoutState();

        Task<int> UpdateAccessKeysAsync(string json, int storeId);

        Task<Address> CreateAddressAsync(CheckoutSessionResponse session, Customer customer, bool createBillingAddress);
    }
}
