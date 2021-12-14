using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;

namespace Smartstore.AmazonPay.Services
{
    public partial class AmazonPayService : IAmazonPayService
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;

        public AmazonPayService(
            SmartDbContext db,
            ICommonServices services)
        {
            _db = db;
            _services = services;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        /// <summary>
        /// Also named "spId".
        /// </summary>
        internal static string PlatformId => "A3OJ83WFYM72IY";
        internal static string LeadCode => "SPEXDEAPA-SmartStore.Net-CP-DP";

        public Task RunDataPollingAsync(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<int> UpdateAccessKeysAsync(string json, int storeId)
        {
            if (json.IsEmpty())
            {
                throw new SmartException(T("Plugins.Payments.AmazonPay.MissingPayloadParameter"));
            }

            dynamic jsonData = JObject.Parse(json);
            var settings = await _services.SettingFactory.LoadSettingsAsync<AmazonPaySettings>(storeId);

            var encryptedPayload = (string)jsonData.encryptedPayload;
            if (encryptedPayload.HasValue())
            {
                throw new SmartException(T("Plugins.Payments.AmazonPay.EncryptionNotSupported"));
            }

            settings.SellerId = (string)jsonData.merchant_id;
            settings.AccessKey = (string)jsonData.access_key;
            settings.SecretKey = (string)jsonData.secret_key;
            settings.ClientId = (string)jsonData.client_id;
            //settings.ClientSecret = (string)json.client_secret;

            return await _services.SettingFactory.SaveSettingsAsync(settings, storeId);
        }

        #region Utilities

        internal static string GetAmazonLanguageCode(string twoLetterLanguageCode, char delimiter = '-')
        {
            return twoLetterLanguageCode.EmptyNull().ToLower() switch
            {
                "en" => $"en{delimiter}GB",
                "fr" => $"fr{delimiter}FR",
                "it" => $"it{delimiter}IT",
                "es" => $"es{delimiter}ES",
                _ => $"de{delimiter}DE",
            };
        }


        #endregion
    }
}
