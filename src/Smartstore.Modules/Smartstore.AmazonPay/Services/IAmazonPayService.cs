using Amazon.Pay.API.WebStore.CheckoutSession;
using Smartstore.Core.Identity;

namespace Smartstore.AmazonPay.Services
{
    public interface IAmazonPayService
    {
        Task<bool> AddCustomerOrderNoteLoopAsync(AmazonPayActionState state, CancellationToken cancelToken = default);

        Task<int> UpdateAccessKeysAsync(string json, int storeId);

        Task<CheckoutAdressResult> CreateAddressAsync(CheckoutSessionResponse session, Customer customer, bool createBillingAddress);
    }
}
