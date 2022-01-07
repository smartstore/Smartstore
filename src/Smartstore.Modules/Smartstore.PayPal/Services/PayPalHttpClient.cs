using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Smartstore.Core;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Stores;
using Smartstore.PayPal.Infrastructure.PayPalObjects;
using Smartstore.PayPal.Settings;

namespace Smartstore.PayPal.Services
{
    public class PayPalHttpClient
    {
        private const string ApiUrlLive = "https://api-m.paypal.com";
        private const string ApiUrlSandbox = "https://api-m.sandbox.paypal.com";

        // TODO: (mh) (core) Maybe create own class for this, including endpoint enum 
        /// <summary>
        /// {0} ApiUrl
        /// {1} OrderId || AuthorizationId || CaptureId
        /// </summary>
        private const string OrderEndpoint                  = "{0}/v2/checkout/orders/{1}";
        private const string AuthorizeEndpoint              = "{0}/v2/checkout/orders/{1}/authorize";
        private const string CaptureEndpoint                = "{0}/v2/checkout/orders/{1}/capture";
        private const string CaptureAuthorizationEndpoint   = "{0}/v2/payments/authorizations/{1}/capture";
        private const string VoidEndpoint                   = "{0}/v2/payments/authorizations/{1}/void";
        private const string RefundEndpoint                 = "{0}/v2/payments/captures/{1}/refund";
        private const string TokenEndpoint                  = "{0}/v1/oauth2/token";

        private readonly HttpClient _client;
        private readonly ILogger _logger;
        private readonly PayPalSettings _settings;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PayPalHttpClient(
            HttpClient client,
            ILogger logger,
            PayPalSettings settings,
            ICheckoutStateAccessor checkoutStateAccessor,
            IHttpContextAccessor httpContextAccessor)
        {
            _client = client;
            _logger = logger;
            _settings = settings;
            _checkoutStateAccessor = checkoutStateAccessor;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// For testing purposes only!
        /// </summary>
        public async Task GetOrder(ProcessPaymentRequest processPaymentRequest)
        {
            string error = null;
            HttpResponseMessage responseMessage = null;

            try
            {
                await EnsureAuthorizationAsync();
                var url = GetEndPointUrl(PayPalTransaction.UpdateOrder, processPaymentRequest.PaypalOrderId);
                responseMessage = await _client.GetAsync(url);
            }
            catch (Exception exception)
            {
                error = exception.ToString();
            }

            if (responseMessage != null)
            {
                var rawResponse = await responseMessage.Content.ReadAsStringAsync();

                // TODO: (mh) (core) Store some info from response

                if (responseMessage.StatusCode == HttpStatusCode.OK || responseMessage.StatusCode == HttpStatusCode.Accepted)
                {
                    dynamic response = JObject.Parse(rawResponse);

                    error = (string)response.error;
                    if (error.IsEmpty() && response.messages != null)
                    {
                        error = response.messages[0].error;
                    }
                }
                else
                {
                    error = rawResponse;
                }
            }

            if (error.HasValue())
            {
                _logger.Error(error);
            }
        }

        /// <summary>
        /// Updates paypal order before shop order confirmation.
        /// </summary>
        public async Task UpdateOrder(ProcessPaymentRequest request, ProcessPaymentResult result)
        {
            var storeContext = _httpContextAccessor.HttpContext.RequestServices.GetService<IStoreContext>();
            var store = storeContext.GetStoreById(request.StoreId);
            string error = null;
            HttpResponseMessage responseMessage = null;

            // TODO: (mh) (core) Add more info, like shipping cost, discount & payment fees
            var amount = new AmountWithBreakdown
            {
                Value = request.OrderTotal.ToString("0.00", CultureInfo.InvariantCulture),
                CurrencyCode = store.PrimaryStoreCurrency.CurrencyCode
            };

            var patches = new List<Patch<object>>
            {
                new Patch<object>
                {
                    Op = "replace",
                    Path = "/purchase_units/@reference_id=='default'/amount",
                    Value = amount
                }
            };

            var data = JsonConvert.SerializeObject(patches, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });

            try
            {
                await EnsureAuthorizationAsync();

                var url = GetEndPointUrl(PayPalTransaction.UpdateOrder, request.PaypalOrderId);
                var requestMessage = new HttpRequestMessage(HttpMethod.Patch, url)
                {
                    Content = new StringContent(data, Encoding.UTF8, "application/json")
                };
                responseMessage = await _client.SendAsync(requestMessage);
            }
            catch (Exception exception)
            {
                error = exception.ToString();
            }

            if (responseMessage != null)
            {
                if (responseMessage.StatusCode != HttpStatusCode.NoContent && responseMessage.IsSuccessStatusCode != true)
                {
                    error = $"{responseMessage.StatusCode} {responseMessage.ReasonPhrase}";
                    result.Errors.Add(error);
                }
            }

            HandleError(error, result);
        }

        /// <summary>
        /// Creates payment for an order by calling capture or authorize according to corresponding setting. 
        /// </summary>
        public async Task DoCheckout(ProcessPaymentRequest processPaymentRequest, ProcessPaymentResult result)
        {
            string error = null;
            HttpResponseMessage responseMessage = null;

            try
            {
                await EnsureAuthorizationAsync();

                var url = _settings.Intent == "authorize"
                    ? GetEndPointUrl(PayPalTransaction.Authorize, processPaymentRequest.PaypalOrderId)
                    : GetEndPointUrl(PayPalTransaction.Capture, processPaymentRequest.PaypalOrderId);

                // TODO: (mh) (core) No need to post as json, when you don't need to transmit JSON.
                responseMessage = await _client.PostAsJsonAsync(url, new Dictionary<string, object>());
            }
            catch (Exception exception)
            {
                error = exception.ToString();
            }

            if (responseMessage != null)
            {
                var rawResponse = await responseMessage.Content.ReadAsStringAsync();

                if (responseMessage.StatusCode == HttpStatusCode.Created)
                {
                    dynamic response = JObject.Parse(rawResponse);
                    
                    if (_settings.Intent == "authorize") 
                    {
                        result.AuthorizationTransactionId = (string)response.purchase_units[0].payments.authorizations[0].id;
                        result.AuthorizationTransactionResult = (string)response.status;
                        result.NewPaymentStatus = PaymentStatus.Authorized;
                    }
                    else
                    {
                        result.CaptureTransactionId = (string)response.purchase_units[0].payments.captures[0].id;
                        result.CaptureTransactionResult = (string)response.status;
                        result.NewPaymentStatus = PaymentStatus.Paid;
                    }
                }
                else
                {
                    error = rawResponse;
                }
            }

            HandleError(error, result);
        }

        /// <summary>
        /// Captures authorized payment.
        /// </summary>
        public async Task CapturePayment(CapturePaymentRequest capturePaymentRequest, CapturePaymentResult result)
        {
            string error = null;
            HttpResponseMessage responseMessage = null;

            try
            {
                await EnsureAuthorizationAsync();
                var url = GetEndPointUrl(PayPalTransaction.CaptureAuthorization, capturePaymentRequest.Order.AuthorizationTransactionId);
                responseMessage = await _client.PostAsJsonAsync(url, new Dictionary<string, object>());
            }
            catch (Exception exception)
            {
                error = exception.ToString();
            }

            if (responseMessage != null)
            {
                if (responseMessage.StatusCode == HttpStatusCode.Created)
                {
                    var rawResponse = await responseMessage.Content.ReadAsStringAsync();
                    dynamic response = JObject.Parse(rawResponse);

                    result.NewPaymentStatus = PaymentStatus.Paid;
                    result.CaptureTransactionId = (string)response.id;
                    result.CaptureTransactionResult = (string)response.status;
                }
                else
                {
                    error = responseMessage.ReasonPhrase;
                }
            }

            HandleError(error, result);
        }

        /// <summary>
        /// Voids authorized payment.
        /// </summary>
        public async Task VoidPayment(VoidPaymentRequest request, VoidPaymentResult result)
        {
            string error = null;
            HttpResponseMessage responseMessage = null;

            try
            {
                await EnsureAuthorizationAsync();
                var url = GetEndPointUrl(PayPalTransaction.Void, request.Order.AuthorizationTransactionId);
                responseMessage = await _client.PostAsJsonAsync(url, new Dictionary<string, object>());
            }
            catch (Exception exception)
            {
                error = exception.ToString();
            }

            if (responseMessage != null)
            {
                if (responseMessage.StatusCode == HttpStatusCode.NoContent)
                {
                    result.NewPaymentStatus = PaymentStatus.Voided;
                }
                else
                {
                    error = responseMessage.ReasonPhrase;
                }
            }

            HandleError(error, result);
        }

        /// <summary>
        /// Refunds captured payment.
        /// </summary>
        public async Task RefundPayment(RefundPaymentRequest request, RefundPaymentResult result)
        {
            string error = null;
            HttpResponseMessage responseMessage = null;

            try
            {
                var amount = new Money();
                if (request.IsPartialRefund)
                {
                    var storeContext = _httpContextAccessor.HttpContext.RequestServices.GetService<IStoreContext>();
                    var store = storeContext.GetStoreById(request.Order.StoreId);

                    amount.Value = request.AmountToRefund.Amount.ToString("0.00", CultureInfo.InvariantCulture);
                    amount.CurrencyCode = store.PrimaryStoreCurrency.CurrencyCode;
                }

                var data = JsonConvert.SerializeObject(amount, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });

                await EnsureAuthorizationAsync();
                var url = GetEndPointUrl(PayPalTransaction.Refund, request.Order.CaptureTransactionId);
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(data, Encoding.UTF8, "application/json")
                };
                responseMessage = await _client.SendAsync(requestMessage);
            }
            catch (Exception exception)
            {
                error = exception.ToString();
            }

            if (responseMessage != null)
            {
                if (responseMessage.StatusCode == HttpStatusCode.NoContent || responseMessage.StatusCode == HttpStatusCode.Created)
                {
                    if (request.IsPartialRefund)
                    {
                        result.NewPaymentStatus = PaymentStatus.PartiallyRefunded;
                    }
                    else
                    {
                        result.NewPaymentStatus = PaymentStatus.Refunded;
                    }
                }
                else
                {
                    error = responseMessage.ReasonPhrase;
                }
            }

            HandleError(error, result);
        }

        /// <summary>
        /// Creates billing plan. (For future use)
        /// </summary>
        public async Task PrepareRecurringPayment(int storeId, Product product)
        {
            string error = null;
            HttpResponseMessage responseMessage = null;

            try
            {
                // TODO: (mh) (core) Create product & store returned product id as GenericAttribute for shop product
                // https://developer.paypal.com/api/catalog-products/v1/#products_create

                var services = _httpContextAccessor.HttpContext.RequestServices.GetService<ICommonServices>();
                var store = services.StoreContext.GetStoreById(storeId);
                var billingPlanName = services.Localization.GetResource("TODO.BillingPlanName").FormatWith("TODO:Productname");

                var billingPlan = new BillingPlan
                {
                    Name = billingPlanName,
                    Description = billingPlanName,
                    Type = "FIXED"  // Smartstore doesn't support infinite cycles
                };

                var paymentDefinition = new PaymentDefinition
                {
                    Name = billingPlanName,
                    Cycles = product.RecurringTotalCycles.ToString(),
                    FrequencyInterval = product.RecurringCyclePeriod.ToString(),
                    Amount = new Money{
                        // TODO: (mh) (core) Respect discounts & more?
                        Value = product.Price.ToString("0.00", CultureInfo.InvariantCulture),
                        CurrencyCode = store.PrimaryStoreCurrency.CurrencyCode
                    }
                };

                paymentDefinition.Frequency = product.RecurringCyclePeriod switch
                {
                    RecurringProductCyclePeriod.Days    => "DAY",
                    RecurringProductCyclePeriod.Weeks   => "WEEK",
                    RecurringProductCyclePeriod.Months  => "MONTH",
                    RecurringProductCyclePeriod.Years   => "YEAR",
                    _ => throw new SmartException("Period not supported."),
                };

                billingPlan.PaymentDefinitions.Add(paymentDefinition);

                var data = JsonConvert.SerializeObject(billingPlan, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });

                await EnsureAuthorizationAsync();
                // TODO: (mh) (core) Make request & store plan id as GenericAttribute for shop product
                // TODO: (mh) (core) Don't forget to delete Attributes when product properties for Recurrency are changing
                //                   or product gets deletet.
            }
            catch (Exception exception)
            {
                error = exception.ToString();
            }

            if (responseMessage != null)
            {
                if (responseMessage.StatusCode == HttpStatusCode.NoContent || responseMessage.StatusCode == HttpStatusCode.Created)
                {
                    // TODO: (mh) (core) Handle response 
                }
                else
                {
                    error = responseMessage.ReasonPhrase;
                }
            }

            // TODO: (mh) (core) Handle error
        }

        private void HandleError(string error, PaymentResult result)
        {
            if (error.HasValue())
            {
                result.Errors.Add(error);
                _logger.Error(error);
            }
        }

        private async Task EnsureAuthorizationAsync()
        {
            // TODO: (mh) (core) This must be stored in session & only obtained anew if it is invalid.
            var accessToken = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var encodedAuthDetails = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.ClientId}:{_settings.Secret}"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedAuthDetails);

            var requestBody = new StringContent("grant_type=client_credentials");
            requestBody.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await _client.PostAsync(GetEndPointUrl(PayPalTransaction.Token), requestBody);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            dynamic token = JObject.Parse(responseBody);

            return token.access_token;
        }

        private string GetEndPointUrl(PayPalTransaction transaction, string id = "")
        {
            var url = string.Empty;
            var apiUrl = _settings.UseSandbox ? ApiUrlSandbox : ApiUrlLive;

            switch (transaction)
            {
                case PayPalTransaction.UpdateOrder:
                    url = OrderEndpoint.FormatInvariant(apiUrl, id);
                    break;
                case PayPalTransaction.Authorize:
                    url = AuthorizeEndpoint.FormatInvariant(apiUrl, id);
                    break;
                case PayPalTransaction.Capture:
                    url = CaptureEndpoint.FormatInvariant(apiUrl, id);
                    break;
                case PayPalTransaction.CaptureAuthorization:
                    url = CaptureAuthorizationEndpoint.FormatInvariant(apiUrl, id);
                    break;
                case PayPalTransaction.Void:
                    url = VoidEndpoint.FormatInvariant(apiUrl, id);
                    break;
                case PayPalTransaction.Refund:
                    url = RefundEndpoint.FormatInvariant(apiUrl, id);
                    break;
                case PayPalTransaction.Token:
                    url = TokenEndpoint.FormatInvariant(apiUrl);
                    break;
            }

            return url;
        }

        private enum PayPalTransaction
        {
            UpdateOrder,
            Authorize,
            Capture,
            CaptureAuthorization,
            Void,
            Refund,
            Token
        };
    }
}