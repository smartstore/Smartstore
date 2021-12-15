using AmazonPay;

namespace Smartstore.AmazonPay.Services
{
    public interface IAmazonPayService
    {
        Task RunDataPollingAsync(CancellationToken cancelToken = default);
        Task<bool> AddCustomerOrderNoteLoopAsync(AmazonPayActionState state, CancellationToken cancelToken = default);

        bool HasCheckoutState();
        AmazonPayCheckoutState GetCheckoutState();

        Task<int> UpdateAccessKeysAsync(string json, int storeId);

        string GetAmazonLanguageCode(string twoLetterLanguageCode = null, char delimiter = '-');
        Regions.currencyCode GetAmazonCurrencyCode(string currencyCode = null);

        Client CreateApiClient(AmazonPaySettings settings);
    }
}
