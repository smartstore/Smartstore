namespace Smartstore.AmazonPay.Services
{
    public interface IAmazonPayService
    {
        Task<bool> AddCustomerOrderNoteLoopAsync(AmazonPayActionState state, CancellationToken cancelToken = default);

        bool HasCheckoutState();
        AmazonPayCheckoutState GetCheckoutState();

        Task<int> UpdateAccessKeysAsync(string json, int storeId);

        string GetAmazonLanguageCode(string twoLetterLanguageCode = null, char delimiter = '-');
    }
}
