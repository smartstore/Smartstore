using System.Linq;
using AmazonPay;
using AmazonPay.CommonRequests;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Utilities;

namespace Smartstore.AmazonPay.Services
{
    public partial class AmazonPayService : IAmazonPayService
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;

        private readonly Currency _primaryCurrency;

        public AmazonPayService(
            SmartDbContext db,
            ICommonServices services,
            ICurrencyService currencyService,
            ICheckoutStateAccessor checkoutStateAccessor)
        {
            _db = db;
            _services = services;
            _checkoutStateAccessor = checkoutStateAccessor;

            _primaryCurrency = currencyService.PrimaryCurrency;
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

        public Task<bool> AddCustomerOrderNoteLoopAsync(AmazonPayActionState state, CancellationToken cancelToken = default)
        {
            throw new NotImplementedException();
        }

        public bool HasCheckoutState()
        {
            var checkoutStateKey = AmazonPayProvider.SystemName + ".CheckoutState";
            var checkoutState = _checkoutStateAccessor.CheckoutState;

            if (checkoutState != null && checkoutState.CustomProperties.ContainsKey(checkoutStateKey))
            {
                return checkoutState.CustomProperties[checkoutStateKey] is AmazonPayCheckoutState state && state.AccessToken.HasValue();
            }

            return false;
        }

        public AmazonPayCheckoutState GetCheckoutState()
        {
            var checkoutState = _checkoutStateAccessor.CheckoutState;

            if (checkoutState == null)
            {
                throw new SmartException(T("Plugins.Payments.AmazonPay.MissingCheckoutSessionState"));
            }

            if (checkoutState.CustomProperties.Get(AmazonPayProvider.SystemName + ".CheckoutState") is not AmazonPayCheckoutState state)
            {
                throw new SmartException(T("Plugins.Payments.AmazonPay.MissingCheckoutSessionState"));
            }

            return state;
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

        public string GetAmazonLanguageCode(string twoLetterLanguageCode = null, char delimiter = '-')
        {
            twoLetterLanguageCode ??= _services.WorkContext.WorkingLanguage.UniqueSeoCode;

            return twoLetterLanguageCode.EmptyNull().ToLower() switch
            {
                "en" => $"en{delimiter}GB",
                "fr" => $"fr{delimiter}FR",
                "it" => $"it{delimiter}IT",
                "es" => $"es{delimiter}ES",
                _ => $"de{delimiter}DE",
            };
        }

        public Regions.currencyCode GetAmazonCurrencyCode(string currencyCode = null)
        {
            currencyCode ??= _primaryCurrency.CurrencyCode;

            return currencyCode.EmptyNull().ToLower() switch
            {
                "usd" => Regions.currencyCode.USD,
                "gbp" => Regions.currencyCode.GBP,
                "jpy" => Regions.currencyCode.JPY,
                "aud" => Regions.currencyCode.AUD,
                "zar" => Regions.currencyCode.ZAR,
                "chf" => Regions.currencyCode.CHF,
                "nok" => Regions.currencyCode.NOK,
                "dkk" => Regions.currencyCode.DKK,
                "sek" => Regions.currencyCode.SEK,
                "nzd" => Regions.currencyCode.NZD,
                "hkd" => Regions.currencyCode.HKD,
                _ => Regions.currencyCode.EUR,
            };
        }

        public Client CreateApiClient(AmazonPaySettings settings)
        {
            var module = _services.ApplicationContext.ModuleCatalog.GetModuleByName("Smartstore.AmazonPay");
            var appVersion = module?.Version?.ToString() ?? "1.0";

            var region = settings.Marketplace.EmptyNull().ToLower() switch
            {
                "us" => Regions.supportedRegions.us,
                "uk" => Regions.supportedRegions.uk,
                "jp" => Regions.supportedRegions.jp,
                _ => Regions.supportedRegions.de,
            };

            var config = new Configuration()
                .WithAccessKey(settings.AccessKey)
                .WithClientId(settings.ClientId)
                .WithSecretKey(settings.SecretKey)
                .WithSandbox(settings.UseSandbox)
                .WithApplicationName("Smartstore " + AmazonPayProvider.SystemName)
                .WithApplicationVersion(appVersion)
                .WithRegion(region);

            var client = new Client(config);
            return client;
        }

        #region Utilities

        internal static string GenerateRandomId(string prefix)
        {
            var str = prefix + CommonHelper.GenerateRandomDigitCode(20);
            return str.Truncate(32);
        }

        private static void GetFirstAndLastName(string name, out string firstName, out string lastName)
        {
            if (name.HasValue())
            {
                var index = name.LastIndexOf(' ');

                if (index == -1)
                {
                    firstName = string.Empty;
                    lastName = name;
                }
                else
                {
                    firstName = name[..index];
                    lastName = name[(index + 1)..];
                }

                firstName = firstName.EmptyNull().Truncate(4000);
                lastName = lastName.EmptyNull().Truncate(4000);
            }
            else
            {
                firstName = lastName = string.Empty;
            }
        }

        private static Address FindAddress(List<Address> addresses, Address address)
        {
            var match = addresses.FindAddress(address);

            if (match == null)
            {
                // Also check incomplete "ToAddress".
                match = addresses.FirstOrDefault(x =>
                    x.FirstName == null && x.LastName == null &&
                    x.Address1 == null && x.Address2 == null &&
                    x.City == address.City && x.ZipPostalCode == address.ZipPostalCode &&
                    x.PhoneNumber == null &&
                    x.CountryId == address.CountryId && x.StateProvinceId == address.StateProvinceId
                );
            }

            return match;
        }

        private async Task<Order> FindOrderByAmazonId(string amazonId)
        {
            // S02-9777218-8608106				OrderReferenceId
            // S02-9777218-8608106-A088344		Auth ID
            // S02-9777218-8608106-C088344		Capture ID

            if (amazonId.HasValue())
            {
                var amazonOrderReferenceId = amazonId[..amazonId.LastIndexOf('-')];
                if (amazonOrderReferenceId.HasValue())
                {
                    var orders = await _db.Orders
                        .Where(x => x.PaymentMethodSystemName == AmazonPayProvider.SystemName && x.AuthorizationTransactionId.StartsWith(amazonOrderReferenceId))
                        .ToListAsync();

                    if (orders.Count == 1)
                    {
                        return orders[0];
                    }
                }
            }

            return null;
        }

        #endregion
    }
}
