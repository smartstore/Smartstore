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
        public static WebStoreClient GetAmazonPayApiClient(this HttpContext context, int storeId)
        {
            Guard.NotNull(context, nameof(context));

            return context.GetItem("AmazonPayApiClient" + storeId, () =>
            {
                var settingFactory = context.RequestServices.GetService<ISettingFactory>();
                var settings = settingFactory.LoadSettings<AmazonPaySettings>(storeId);

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
