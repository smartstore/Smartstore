using Amazon.Pay.API.WebStore.CheckoutSession;
using Smartstore.Core.Identity;
using AmazonPayTypes = Amazon.Pay.API.Types;

namespace Smartstore.AmazonPay.Services
{
    public interface IAmazonPayService
    {
        Task<bool> AddCustomerOrderNoteLoopAsync(AmazonPayActionState state, CancellationToken cancelToken = default);

        Task<int> UpdateAccessKeysAsync(string json, int storeId);

        Task<CheckoutAdressResult> CreateAddressAsync(CheckoutSessionResponse session, Customer customer, bool createBillingAddress);
        AmazonPayTypes.Currency GetAmazonPayCurrency(string currencyCode = null);
    }
}
