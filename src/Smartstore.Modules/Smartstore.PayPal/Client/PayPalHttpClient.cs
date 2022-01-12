using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.PayPal.Client.Messages;
using Smartstore.PayPal.Settings;

namespace Smartstore.PayPal.Client
{
    public class PayPalHttpClient
    {
        private const string ApiUrlLive = "https://api-m.paypal.com";
        private const string ApiUrlSandbox = "https://api-m.sandbox.paypal.com";

        /// <summary>
        /// Key for PayPal access token caching
        /// </summary>
        /// <remarks>
        /// {0} : current store ID
        /// </remarks>
        public const string PAYPAL_ACCESS_TOKEN_KEY = "paypal:accesstoken-{0}";
        public const string PAYPAL_ACCESS_TOKEN_PATTERN_KEY = "paypal:accesstoken-*";

        // TODO: (mh) (core) Remove
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
        private readonly IStoreContext _storeContext;
        private readonly ILocalizationService _locService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICacheFactory _cacheFactory;

        public PayPalHttpClient(
            HttpClient client,
            ILogger logger,
            PayPalSettings settings,
            ICheckoutStateAccessor checkoutStateAccessor,
            IStoreContext storeContext,
            ILocalizationService locService,
            IHttpContextAccessor httpContextAccessor,
            ICacheFactory cacheFactory)
        {
            _client = client;
            _logger = logger;
            _settings = settings;
            _checkoutStateAccessor = checkoutStateAccessor;
            _storeContext = storeContext;
            _locService = locService;
            _httpContextAccessor = httpContextAccessor;
            _cacheFactory = cacheFactory;
        }

        #region NEW --> MH

        // TODO: (mh) (core) Refactor & complete PayPalHttpClient/Messages according to this blueprint

        /// <summary>
        /// Gets access token and add authorization header.
        /// </summary>
        async Task SetAccessTokenHeaderAsync(HttpRequestMessage request)
        {
            if (!request.Headers.Contains("Authorization") && request is not AccessTokenRequest)
            {
                // TODO: (mh) (core) Don't forget model invalidation on setting change.
                var cacheKey = string.Format(PAYPAL_ACCESS_TOKEN_KEY, _storeContext.CurrentStore.Id);
                var token = await GetAccessTokenFromCacheAsync();

                // Should never happen, but just to be save...
                if (token.IsExpired())
                {
                    // Invalidate cache & obtain new token.
                    _cacheFactory.GetMemoryCache().Remove(cacheKey);
                    token = await GetAccessTokenFromCacheAsync();
                }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            }
        }

        /// <summary>
        /// Gets access token from memory cache.
        /// </summary>
        private async Task<AccessToken> GetAccessTokenFromCacheAsync()
        {
            var cacheKey = string.Format(PAYPAL_ACCESS_TOKEN_KEY, _storeContext.CurrentStore.Id);
            var token = await _cacheFactory.GetMemoryCache().GetAsync(cacheKey, async (o) =>
            {
                var accessTokenRequest = new AccessTokenRequest(_settings.ClientId, _settings.Secret);
                var response = await Execute(accessTokenRequest);
                var accesstoken = response.Body<AccessToken>();

                o.ExpiresIn(TimeSpan.FromSeconds(accesstoken.ExpiresIn - 30));
                o.SetSlidingExpiration(TimeSpan.FromHours(6));

                return accesstoken;
            });

            return token;
        }

        /// <summary>
        /// Authorizes an order.
        /// </summary>
        public async Task<PayPalResponse> UpdateOrderAsync(ProcessPaymentRequest request, ProcessPaymentResult result)
        {
            Guard.NotNull(request, nameof(request));

            var ordersPatchRequest = new OrdersPatchRequest<object>(request.PaypalOrderId);
            
            await SetAccessTokenHeaderAsync(ordersPatchRequest);

            // TODO: (mh) (core) Add more info, like shipping cost, discount & payment fees
            var store = _storeContext.GetStoreById(request.StoreId);
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

            ordersPatchRequest.WithBody(patches);

            var response = await Execute(ordersPatchRequest);

            if (response.Status == HttpStatusCode.Created)
            {
                var rawResponse = response.Body<object>().ToString();
                dynamic jResponse = JObject.Parse(rawResponse);
                
                // TODO: (mh) (core) What to do here? Return success or error & onyl proceed if successful???
            }

            return response;
        }

        /// <summary>
        /// Authorizes an order.
        /// </summary>
        public async Task<PayPalResponse> AuthorizeOrderAsync(ProcessPaymentRequest request, ProcessPaymentResult result)
        {
            Guard.NotNull(request, nameof(request));

            var ordersAuthorizeRequest = new OrdersAuthorizeRequest(request.PaypalOrderId);

            await SetAccessTokenHeaderAsync(ordersAuthorizeRequest);

            var response = await Execute(ordersAuthorizeRequest);

            // TODO: (mh) (core) Make it so.
            var rawResponse = response.Body<object>().ToString();
            dynamic jResponse = JObject.Parse(rawResponse);

            result.AuthorizationTransactionId = (string)jResponse.purchase_units[0].payments.authorizations[0].id;
            result.AuthorizationTransactionResult = (string)jResponse.status;
            result.NewPaymentStatus = PaymentStatus.Authorized;

            return response;
        }

        /// <summary>
        /// Captures an order.
        /// </summary>
        public async Task<PayPalResponse> CaptureOrderAsync(ProcessPaymentRequest request, ProcessPaymentResult result)
        {
            Guard.NotNull(request, nameof(request));

            var ordersCaptureRequest = new OrdersCaptureRequest(request.PaypalOrderId);

            await SetAccessTokenHeaderAsync(ordersCaptureRequest);

            var response = await Execute(ordersCaptureRequest);
            var rawResponse = response.Body<object>().ToString();

            dynamic jResponse = JObject.Parse(rawResponse);

            result.CaptureTransactionId = (string)jResponse.purchase_units[0].payments.captures[0].id;
            result.CaptureTransactionResult = (string)jResponse.status;
            result.NewPaymentStatus = PaymentStatus.Paid;

            return response;
        }

        /// <summary>
        /// Captures authorized payment.
        /// </summary>
        public async Task<PayPalResponse> CapturePaymentAsync(CapturePaymentRequest request, CapturePaymentResult result)
        {
            Guard.NotNull(request, nameof(request));

            // TODO: (mh) (core) If ERPs are used this ain't the real Invoice-Id > Make optional or remove (TBD with MC)
            var message = new CaptureMessage { InvoiceId = request.Order.OrderNumber };
            var voidRequest = new AuthorizationsCaptureRequest(request.Order.CaptureTransactionId)
                .WithBody(message);

            await SetAccessTokenHeaderAsync(voidRequest);

            var response = await Execute(voidRequest);
            var capture = response.Body<Capture>();

            result.NewPaymentStatus = PaymentStatus.Paid;
            result.CaptureTransactionId = capture.Id;
            result.CaptureTransactionResult = capture.Status;

            return response;
        }

        /// <summary>
        /// Voids authorized payment.
        /// </summary>
        public async Task<PayPalResponse> VoidPaymentAsync(VoidPaymentRequest request, VoidPaymentResult result)
        {
            Guard.NotNull(request, nameof(request));

            var voidRequest = new AuthorizationsVoidRequest(request.Order.CaptureTransactionId);

            await SetAccessTokenHeaderAsync(voidRequest);

            var response = await Execute(voidRequest);

            result.NewPaymentStatus = PaymentStatus.Voided;

            return response;
        }

        /// <summary>
        /// Refunds captured payment.
        /// </summary>
        public async Task<PayPalResponse> RefundPaymentAsync(RefundPaymentRequest request, RefundPaymentResult result)
        {
            Guard.NotNull(request, nameof(request));
            
            var message = new RefundMessage();

            if (request.IsPartialRefund)
            {
                var store = _storeContext.GetStoreById(request.Order.StoreId);

                message.Amount = new MoneyMessage
                {
                    Value = request.AmountToRefund.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                    CurrencyCode = store.PrimaryStoreCurrency.CurrencyCode
                };
            }

            var refundRequest = new CapturesRefundRequest(request.Order.CaptureTransactionId)
                .WithBody(message);
            
            await SetAccessTokenHeaderAsync(refundRequest);

            var response = await Execute(refundRequest);

            result.NewPaymentStatus = request.IsPartialRefund
                ? PaymentStatus.PartiallyRefunded
                : PaymentStatus.Refunded;

            return response;
        }

        // TODO: (mh) (core) Copy to other methods
        public Task<PayPalResponse> RefundPayment(CapturesRefundRequest request)
        {
            return Execute(request);
        }

        #endregion

        #region Infrastructure

        public virtual async Task<PayPalResponse> Execute<TRequest>(TRequest request, CancellationToken cancelToken = default)
            where TRequest : PayPalRequest
        {
            Guard.NotNull(request, nameof(request));

            request = request.Clone<TRequest>();

            var apiUrl = _settings.UseSandbox ? ApiUrlSandbox : ApiUrlLive;
            request.RequestUri = new Uri(apiUrl + request.Path.EnsureStartsWith('/'));

            if (request.Body != null)
            {
                request.Content = SerializeRequest(request);
            }
            else
            {
                // Support empty messages
                request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
            }

            var response = await _client.SendAsync(request, cancelToken);

            if (response.IsSuccessStatusCode)
            {
                object responseBody = null;

                if (response.Content.Headers.ContentType != null)
                {
                    responseBody = await DeserializeResponseAsync(response.Content, request.ResponseType);
                }

                return new PayPalResponse(response.StatusCode, response.Headers, responseBody);
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancelToken);
                throw new PayPalException(responseBody, new PayPalResponse(
                    response.StatusCode, 
                    response.Headers,
                    responseBody));
            }
        }

        protected HttpContent SerializeRequest(PayPalRequest request)
        {
            if (request.ContentType == null)
            {
                throw new IOException("HttpRequest did not have content-type header set");
            }

            request.ContentType = request.ContentType.ToLower();

            HttpContent content = null;

            if (request.ContentType == "application/json")
            {
                var json = JsonConvert.SerializeObject(request.Body);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            else if (request.ContentType == "application/x-www-form-urlencoded")
            {
                content = new FormUrlEncodedContent((IEnumerable<KeyValuePair<string, string>>)request.Body);
            }
            // TODO: (mh) (core) Is TextSerializer necessary?

            if (content == null)
            {
                throw new IOException($"Unable to serialize request with Content-Type {request.ContentType} because it is not supported.");
            }

            return content;
        }

        protected async Task<object> DeserializeResponseAsync(HttpContent content, Type responseType)
        {
            if (content.Headers.ContentType == null)
            {
                throw new IOException("HTTP response did not have content-type header set");
            }

            var contentType = content.Headers.ContentType.ToString().ToLower();

            if (contentType == "application/json")
            {
                var message = JsonConvert.DeserializeObject(await content.ReadAsStringAsync(), responseType);
                return message;
            }
            else
            {
                // TODO: (mh) (core) Is TextSerializer necessary?
                throw new IOException($"Unable to deserialize response with Content-Type {contentType} because it is not supported.");
            }
        }

        private void HandleException(Exception exception, PaymentResult result, bool log = false)
        {
            if (exception != null)
            {
                result.Errors.Add(exception.Message);
                if (log)
                    _logger.Error(exception);
            }
        }

        #endregion

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
            var store = _storeContext.GetStoreById(request.StoreId);
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

            var data = JsonConvert.SerializeObject(patches);

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

        // TODO: (mh) (core) Remove
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

        // TODO: (mh) (core) Remove
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

        // TODO: (mh) (core) Remove
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

        // TODO: (mh) (core) Remove
        /// <summary>
        /// Refunds captured payment.
        /// </summary>
        public async Task RefundPayment(RefundPaymentRequest request, RefundPaymentResult result)
        {
            string error = null;
            HttpResponseMessage responseMessage = null;

            try
            {
                var amount = new MoneyMessage();
                if (request.IsPartialRefund)
                {
                    var store = _storeContext.GetStoreById(request.Order.StoreId);

                    amount.Value = request.AmountToRefund.Amount.ToString("0.00", CultureInfo.InvariantCulture);
                    amount.CurrencyCode = store.PrimaryStoreCurrency.CurrencyCode;
                }

                var data = JsonConvert.SerializeObject(amount);

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

                var store = _storeContext.GetStoreById(storeId);
                var billingPlanName = _locService.GetResource("TODO.BillingPlanName").FormatWith("TODO:Productname");

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
                    Amount = new MoneyMessage{
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

                var data = JsonConvert.SerializeObject(billingPlan);

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

        // TODO: (mh) (core) Remove
        private async Task EnsureAuthorizationAsync()
        {
            // TODO: (mh) (core) This must be stored in session & only obtained anew if it is invalid.
            var accessToken = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        // TODO: (mh) (core) Remove
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

        // TODO: (mh) (core) Remove
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

        // TODO: (mh) (core) Remove
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