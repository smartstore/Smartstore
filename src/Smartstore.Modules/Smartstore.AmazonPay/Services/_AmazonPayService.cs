using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Messaging;
using Smartstore.Utilities;

namespace Smartstore.AmazonPay.Services
{
    public partial class AmazonPayService : IAmazonPayService
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IMessageFactory _messageFactory;

        private readonly Currency _primaryCurrency;

        public AmazonPayService(
            SmartDbContext db,
            ICommonServices services,
            ICurrencyService currencyService,
            ICheckoutStateAccessor checkoutStateAccessor,
            IMessageFactory messageFactory)
        {
            _db = db;
            _services = services;
            _checkoutStateAccessor = checkoutStateAccessor;
            _messageFactory = messageFactory;

            _primaryCurrency = currencyService.PrimaryCurrency;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;

        /// <summary>
        /// Also named "spId".
        /// </summary>
        internal static string PlatformId => "A3OJ83WFYM72IY";
        internal static string LeadCode => "SPEXDEAPA-SmartStore.Net-CP-DP";

        public async Task<bool> AddCustomerOrderNoteLoopAsync(AmazonPayActionState state, CancellationToken cancelToken = default)
        {
            if (state == null || state.OrderGuid == Guid.Empty)
            {
                return false;
            }

            try
            {
                const int sleepMillSec = 4000;
                const int loopMillSec = 40000;
                var startTime = DateTime.Now.TimeOfDay;

                for (var i = 0; i < 99 && (DateTime.Now.TimeOfDay.Milliseconds - startTime.Milliseconds) <= loopMillSec; ++i)
                {
                    var order = await _db.Orders
                        .Where(x => x.OrderGuid == state.OrderGuid)
                        .FirstOrDefaultAsync(cancelToken);

                    if (order != null)
                    {
                        using var psb = StringBuilderPool.Instance.Get(out var sb);

                        if (state.Errors?.Any() ?? false)
                        {
                            foreach (var error in state.Errors)
                            {
                                sb.AppendFormat("<p>{0}</p>", error);
                            }
                        }

                        var orderNote = new OrderNote
                        {
                            DisplayToCustomer = true,
                            Note = sb.ToString(),
                            CreatedOnUtc = DateTime.UtcNow,
                        };

                        order.OrderNotes.Add(orderNote);
                        await _db.SaveChangesAsync(cancelToken);

                        await _messageFactory.SendNewOrderNoteAddedCustomerNotificationAsync(orderNote, _services.WorkContext.WorkingLanguage.Id);
                        break;
                    }

                    Thread.Sleep(sleepMillSec);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return false;
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

        //public Regions.currencyCode GetAmazonCurrencyCode(string currencyCode = null)
        //{
        //    currencyCode ??= _primaryCurrency.CurrencyCode;

        //    return currencyCode.EmptyNull().ToLower() switch
        //    {
        //        "usd" => Regions.currencyCode.USD,
        //        "gbp" => Regions.currencyCode.GBP,
        //        "jpy" => Regions.currencyCode.JPY,
        //        "aud" => Regions.currencyCode.AUD,
        //        "zar" => Regions.currencyCode.ZAR,
        //        "chf" => Regions.currencyCode.CHF,
        //        "nok" => Regions.currencyCode.NOK,
        //        "dkk" => Regions.currencyCode.DKK,
        //        "sek" => Regions.currencyCode.SEK,
        //        "nzd" => Regions.currencyCode.NZD,
        //        "hkd" => Regions.currencyCode.HKD,
        //        _ => Regions.currencyCode.EUR,
        //    };
        //}

        //public Client CreateApiClient(AmazonPaySettings settings)
        //{
        //    var module = _services.ApplicationContext.ModuleCatalog.GetModuleByName("Smartstore.AmazonPay");
        //    var appVersion = module?.Version?.ToString() ?? "1.0";

        //    var region = settings.Marketplace.EmptyNull().ToLower() switch
        //    {
        //        "us" => Regions.supportedRegions.us,
        //        "uk" => Regions.supportedRegions.uk,
        //        "jp" => Regions.supportedRegions.jp,
        //        _ => Regions.supportedRegions.de,
        //    };

        //    var config = new Configuration()
        //        .WithAccessKey(settings.AccessKey)
        //        .WithClientId(settings.ClientId)
        //        .WithSecretKey(settings.SecretKey)
        //        .WithSandbox(settings.UseSandbox)
        //        .WithApplicationName("Smartstore " + AmazonPayProvider.SystemName)
        //        .WithApplicationVersion(appVersion)
        //        .WithRegion(region);

        //    var client = new Client(config);
        //    return client;
        //}

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
