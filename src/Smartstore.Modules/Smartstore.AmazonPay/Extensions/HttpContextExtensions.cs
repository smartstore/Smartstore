using Amazon.Pay.API;
using Amazon.Pay.API.WebStore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Configuration;
using AmazonPayTypes = Amazon.Pay.API.Types;

namespace Smartstore.AmazonPay
{
    internal static class HttpContextExtensions
    {
        public static Task<WebStoreClient> GetAmazonPayApiClientAsync(this HttpContext context, int storeId)
        {
            Guard.NotNull(context, nameof(context));

            return context.GetItemAsync("AmazonPayApiClient" + storeId, async () =>
            {
                var settingFactory = context.RequestServices.GetService<ISettingFactory>();
                var settings = await settingFactory.LoadSettingsAsync<AmazonPaySettings>(storeId);

                var region = settings.Marketplace.EmptyNull().ToLower() switch
                {
                    "us" => AmazonPayTypes.Region.UnitedStates,
                    "jp" => AmazonPayTypes.Region.Japan,
                    _ => AmazonPayTypes.Region.Europe,
                };

                var config = new ApiConfiguration(
                    region,
                    settings.UseSandbox ? AmazonPayTypes.Environment.Sandbox : AmazonPayTypes.Environment.Live,
                    settings.PublicKeyId,
                    settings.PrivateKey
                );

                return new WebStoreClient(config);
            });
        }
    }
}
