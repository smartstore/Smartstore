using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Autofac.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Smartstore.Caching;
using Smartstore.Core;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;
using Smartstore.PayPal.Client.Messages;
using Smartstore.Web.Models.Cart;
using static Smartstore.Core.Security.Permissions;

namespace Smartstore.PayPal.Client
{
    public class PayPalHttpClient
    {
        const string ApiUrlLive = "https://api-m.paypal.com";
        const string ApiUrlSandbox = "https://api-m.sandbox.paypal.com";

        /// <summary>
        /// Key for PayPal access token caching
        /// </summary>
        /// <remarks>
        /// {0} : PayPal client ID
        /// </remarks>
        public const string PAYPAL_ACCESS_TOKEN_KEY = "paypal:accesstoken-{0}";
        public const string PAYPAL_ACCESS_TOKEN_PATTERN_KEY = "paypal:accesstoken-*";

        private readonly HttpClient _client;
        private readonly SmartDbContext _db;
        private readonly ILogger _logger;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly ILocalizationService _locService;
        private readonly ICurrencyService _currencyService;
        private readonly IMediaService _mediaService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICacheFactory _cacheFactory;
        private readonly ISettingFactory _settingFactory;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ITaxService _taxService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductService _productService;
        private readonly IOrderCalculationService _orderCalculationService;

        public PayPalHttpClient(
            HttpClient client,
            SmartDbContext db,
            ILogger logger,
            ICheckoutStateAccessor checkoutStateAccessor,
            IStoreContext storeContext,
            IWorkContext workContext,
            ILocalizationService locService,
            ICurrencyService currencyService,
            IMediaService mediaService,
            IHttpContextAccessor httpContextAccessor,
            ICacheFactory cacheFactory,
            ISettingFactory settingFactory,
            IShoppingCartService shoppingCartService,
            ITaxService taxService,
            IPriceCalculationService priceCalculationService,
            IProductService productService,
            IOrderCalculationService orderCalculationService)
        {
            _client = client;
            _db = db;
            _logger = logger;
            _checkoutStateAccessor = checkoutStateAccessor;
            _storeContext = storeContext;
            _workContext = workContext;
            _locService = locService;
            _currencyService = currencyService;
            _mediaService = mediaService;
            _httpContextAccessor = httpContextAccessor;
            _cacheFactory = cacheFactory;
            _settingFactory = settingFactory;
            _shoppingCartService = shoppingCartService;
            _taxService = taxService;
            _priceCalculationService = priceCalculationService;
            _productService = productService;
            _orderCalculationService = orderCalculationService;
        }

        #region Payment processing

        /// <summary>
        /// Gets an order. (For testing purposes only)
        /// </summary>
        public async Task<PayPalResponse> GetOrderAsync(ProcessPaymentRequest request, ProcessPaymentResult result, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request, nameof(request));

            var ordersGetRequest = new OrdersGetRequest(request.PayPalOrderId);
            var response = await ExecuteRequestAsync(ordersGetRequest, cancelToken);
            var rawResponse = response.Body<object>().ToString();

            dynamic jResponse = JObject.Parse(rawResponse);

            return response;
        }

        /// <summary>
        /// Creates a PayPal order.
        /// </summary>
        public async Task<PayPalResponse> CreateOrderAsync(
            ProcessPaymentRequest request, 
            CancellationToken cancelToken = default)
        {
            Guard.NotNull(request, nameof(request));

            // TODO: (mh) (core) Some things mustn't be null.

            var customer = _workContext.CurrentCustomer;
            var store = _storeContext.GetStoreById(request.StoreId);
            var settings = _settingFactory.LoadSettings<PayPalSettings>(request.StoreId);
            var language = _workContext.WorkingLanguage;
            var currency = _workContext.WorkingCurrency;
            var paymentData = _checkoutStateAccessor.CheckoutState.PaymentData;

            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, request.StoreId);
            var model = await cart.MapAsync(
                isEditable: false,
                prepareEstimateShippingIfEnabled: false,
                prepareAndDisplayOrderReviewData: false);

            var logoUrl = store.LogoMediaFileId != 0 ? await _mediaService.GetUrlAsync(store.LogoMediaFileId, 0, store.GetHost(true), false) : string.Empty;

            paymentData.TryGetValueAs<string>("PayPalInvoiceBirthdate", out var birthDate);
            paymentData.TryGetValueAs<string>("PayPalInvoicePhoneNumber", out var phoneNumber);

            if (!paymentData.TryGetValueAs<string>("ClientMetaId", out var clientMetaId))
            {
                return null;
            }

            var purchaseUnitItems = new List<PurchaseUnitItem>();

            var cartProducts = cart.Items.Select(x => x.Item.Product).ToArray();
            var batchContext = _productService.CreateProductBatchContext(cartProducts, null, customer, false);
            var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, customer, currency, batchContext);

            foreach (var item in model.Items)
            {
                var cartItem = cart.Items.Where(x => x.Item.ProductId == item.ProductId).FirstOrDefault();
                var taxRate = await _taxService.GetTaxRateAsync(cartItem.Item.Product);
                var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(cartItem, calculationOptions);
                var (unitPrice, subtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);

                var productName = item.ProductName.Value.Length > 126 ? item.ProductName.Value[..126] : item.ProductName.Value;
                var productDescription = item.ShortDesc.Value.Length > 126 ? item.ShortDesc.Value[..126] : item.ShortDesc.Value;

                purchaseUnitItems.Add(new PurchaseUnitItem
                {
                    UnitAmount = new MoneyMessage
                    {
                        Value = unitPrice.Tax.Value.PriceNet.ToStringInvariant("n2"),
                        CurrencyCode = currency.CurrencyCode
                    },
                    Name = productName,
                    Description = productDescription,
                    Category = item.IsEsd ? "DIGITAL_GOODS" : "PHYSICAL_GOODS",
                    Quantity = item.EnteredQuantity.ToString(),
                    Sku = item.Sku,
                    Tax = new MoneyMessage
                    {
                        Value = unitPrice.Tax.Value.Amount.ToStringInvariant("n2"),
                        CurrencyCode = currency.CurrencyCode
                    },
                    TaxRate = taxRate.Rate.ToStringInvariant("n2")
                });
            }

            // Get subtotal
            var cartSubTotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart, false);
            var subTotalConverted = _currencyService.ConvertFromPrimaryCurrency(cartSubTotal.SubtotalWithoutDiscount.Amount, currency);

            // Get tax
            (Money price, TaxRatesDictionary taxRates) = await _orderCalculationService.GetShoppingCartTaxTotalAsync(cart);
            var cartTax = _currencyService.ConvertFromPrimaryCurrency(price.Amount, currency);

            // Get shipping cost
            var shippingTotal = await _orderCalculationService.GetShoppingCartShippingTotalAsync(cart);

            // Discount
            var cartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart);
            var orderTotalDiscountAmount = new Money();
            if (cartTotal.DiscountAmount > decimal.Zero)
            {
                orderTotalDiscountAmount = _currencyService.ConvertFromPrimaryCurrency(cartTotal.DiscountAmount.Amount, currency);
            }

            var purchaseUnits = new List<PurchaseUnit>
            {
                new PurchaseUnit
                {
                    Amount = new AmountWithBreakdown
                    {
                        Value = request.OrderTotal.ToStringInvariant("n2"),
                        CurrencyCode = currency.CurrencyCode,
                        AmountBreakdown = new AmountBreakdown
                        {
                            ItemTotal = new MoneyMessage
                            {
                                Value = subTotalConverted.Amount.ToStringInvariant("n2"),
                                CurrencyCode = currency.CurrencyCode
                            },
                            Shipping = new MoneyMessage
                            {
                                Value = shippingTotal.ShippingTotal.Value.Amount.ToStringInvariant("n2"),
                                CurrencyCode = currency.CurrencyCode
                            },
                            Discount = new MoneyMessage
                            {
                                Value = orderTotalDiscountAmount.Amount.ToStringInvariant("n2"),
                                CurrencyCode = currency.CurrencyCode
                            },
                            TaxTotal = new MoneyMessage
                            {
                                Value = cartTax.Amount.ToStringInvariant("n2"),
                                CurrencyCode = currency.CurrencyCode
                            }
                        }
                    },
                    //InvoiceId = request.OrderNumber,
                    CustomId = request.OrderGuid.ToString(),
                    Description = string.Empty,
                    Items = purchaseUnitItems.ToArray(),
                    Shipping = new ShippingDetail
                    {
                        ShippingName = new ShippingName
                        {
                            FullName = customer.ShippingAddress.GetFullName()
                        },
                        ShippingAddress = new ShippingAddress
                        {
                            AddressLine1 = customer.ShippingAddress.Address1,
                            AddressLine2 = customer.ShippingAddress.Address2,
                            AdminArea1 = customer.ShippingAddress.StateProvince.Name,
                            AdminArea2 = customer.ShippingAddress.City,
                            PostalCode = customer.ShippingAddress.ZipPostalCode,
                            CountryCode = customer.ShippingAddress.Country.TwoLetterIsoCode
                        }
                    }
                }
            };

            var orderMessage = new OrderMessage
            {
                Intent = "CAPTURE",
                ProcessingInstruction = "ORDER_COMPLETE_ON_PAYMENT_APPROVAL",
                PurchaseUnits = purchaseUnits.ToArray(),
                PaymentSource = new PaymentSource
                {
                    PaymentSourceInvoice = new PaymentSourceInvoice
                    {
                        Name = new NameMessage
                        {
                            GivenName = customer.BillingAddress.FirstName,
                            SurName = customer.BillingAddress.LastName
                        },
                        Email = customer.BillingAddress.Email,
                        BirthDate = birthDate,
                        Phone = new PhoneMessage
                        {
                            NationalNumber = phoneNumber,
                            CountryCode = "49"  // TODO: (mh) (core) ...
                        },
                        BillingAddress = new BillingAddressMessage
                        {
                            AddressLine1 = customer.BillingAddress.Address1,
                            AdminArea2 = customer.BillingAddress.City,
                            PostalCode = customer.BillingAddress.ZipPostalCode,
                            CountryCode = customer.BillingAddress.Country.TwoLetterIsoCode
                        },
                        ExperienceContext = new ExperienceContext
                        {
                            BrandName = store.Name,
                            Locale = language.LanguageCulture,
                            LogoUrl = logoUrl,
                            CustomerServiceInstructions = new string[]
                            {
                                "TODO: SERVice number"
                            }
                        }
                    }
                }
            };

            var ordersGetRequest = new OrderCreateRequest()
                .WithRequestId(Guid.NewGuid().ToString())
                .WithClientMetadataId(clientMetaId.ToString())
                .WithBody(orderMessage);

            var response = await ExecuteRequestAsync(ordersGetRequest, cancelToken);
            var rawResponse = response.Body<object>().ToString();

            dynamic jResponse = JObject.Parse(rawResponse);

            return response;
        }

        /// <summary>
        /// Authorizes an order.
        /// </summary>
        public virtual async Task<PayPalResponse> UpdateOrderAsync(ProcessPaymentRequest request, ProcessPaymentResult result, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request, nameof(request));

            var ordersPatchRequest = new OrdersPatchRequest<object>(request.PayPalOrderId);

            var amount = new AmountWithBreakdown
            {
                Value = request.OrderTotal.ToString("0.00", CultureInfo.InvariantCulture),
                CurrencyCode = _currencyService.PrimaryCurrency.CurrencyCode
            };

            var patches = new List<Patch<object>>
            {
                new Patch<object>
                {
                    Op = "replace",
                    Path = "/purchase_units/@reference_id=='default'/amount",
                    Value = amount
                },
                new Patch<object>
                {
                    Op = "add",
                    Path = "/purchase_units/@reference_id=='default'/custom_id",
                    Value = request.OrderGuid
                }
            };

            ordersPatchRequest.WithBody(patches);

            var response = await ExecuteRequestAsync(ordersPatchRequest, request.StoreId, cancelToken);

            return response;
        }

        public Task<PayPalResponse> UpdateOrderAsync(OrdersPatchRequest<object> request, CancellationToken cancelToken = default)
            => ExecuteRequestAsync(request, cancelToken);

        /// <summary>
        /// Authorizes an order.
        /// </summary>
        public virtual async Task<PayPalResponse> AuthorizeOrderAsync(ProcessPaymentRequest request, ProcessPaymentResult result, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request, nameof(request));

            var ordersAuthorizeRequest = new OrdersAuthorizeRequest(request.PayPalOrderId);
            var response = await ExecuteRequestAsync(ordersAuthorizeRequest, request.StoreId, cancelToken);
            var rawResponse = response.Body<object>().ToString();
            dynamic jResponse = JObject.Parse(rawResponse);

            result.AuthorizationTransactionId = (string)jResponse.purchase_units[0].payments.authorizations[0].id;
            result.AuthorizationTransactionCode = (string)jResponse.id;
            result.AuthorizationTransactionResult = (string)jResponse.status;
            result.NewPaymentStatus = PaymentStatus.Authorized;

            return response;
        }

        public Task<PayPalResponse> AuthorizeOrderAsync(OrdersAuthorizeRequest request, CancellationToken cancelToken = default)
            => ExecuteRequestAsync(request, cancelToken);

        /// <summary>
        /// Captures an order.
        /// </summary>
        public virtual async Task<PayPalResponse> CaptureOrderAsync(ProcessPaymentRequest request, ProcessPaymentResult result, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request, nameof(request));

            var ordersCaptureRequest = new OrdersCaptureRequest(request.PayPalOrderId);
            var response = await ExecuteRequestAsync(ordersCaptureRequest, request.StoreId, cancelToken);
            var rawResponse = response.Body<object>().ToString();

            dynamic jResponse = JObject.Parse(rawResponse);

            result.CaptureTransactionId = (string)jResponse.purchase_units[0].payments.captures[0].id;
            result.AuthorizationTransactionCode = (string)jResponse.id;
            result.CaptureTransactionResult = (string)jResponse.status;
            result.NewPaymentStatus = PaymentStatus.Paid;

            return response;
        }

        public Task<PayPalResponse> CaptureOrderAsync(OrdersCaptureRequest request, CancellationToken cancelToken = default)
            => ExecuteRequestAsync(request, cancelToken);

        /// <summary>
        /// Captures authorized payment.
        /// </summary>
        public virtual async Task<PayPalResponse> CapturePaymentAsync(CapturePaymentRequest request, CapturePaymentResult result, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request, nameof(request));

            // TODO: (mh) (core) If ERPs are used this ain't the real Invoice-Id > Make optional or remove (TBD with MC)
            var message = new CaptureMessage { InvoiceId = request.Order.OrderNumber };
            // CaptureTransactionId is null here. Payment is not captured yet. Maybe you want to use AuthorizationTransactionId?
            var voidRequest = new AuthorizationsCaptureRequest(request.Order.CaptureTransactionId).WithBody(message);
            var response = await ExecuteRequestAsync(voidRequest, request.Order.StoreId, cancelToken);
            var capture = response.Body<Capture>();

            result.NewPaymentStatus = PaymentStatus.Paid;
            result.CaptureTransactionId = capture.Id;
            result.CaptureTransactionResult = capture.Status;

            return response;
        }

        public Task<PayPalResponse> CapturePaymentAsync(AuthorizationsCaptureRequest request, CancellationToken cancelToken = default)
            => ExecuteRequestAsync(request, cancelToken);

        /// <summary>
        /// Voids authorized payment.
        /// </summary>
        public virtual async Task<PayPalResponse> VoidPaymentAsync(VoidPaymentRequest request, VoidPaymentResult result, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request, nameof(request));

            // TODO: (mh) (core) Produces exception "Value cannot be null. Parameter 'stringToEscape'".
            // CaptureTransactionId is null here. Payment is not captured yet. Maybe you want to use AuthorizationTransactionId?
            var voidRequest = new AuthorizationsVoidRequest(request.Order.CaptureTransactionId);
            var response = await ExecuteRequestAsync(voidRequest, request.Order.StoreId, cancelToken);

            result.NewPaymentStatus = PaymentStatus.Voided;

            return response;
        }

        public Task<PayPalResponse> VoidPaymentAsync(AuthorizationsVoidRequest request, CancellationToken cancelToken = default)
            => ExecuteRequestAsync(request, cancelToken);

        /// <summary>
        /// Refunds captured payment.
        /// </summary>
        public virtual async Task<PayPalResponse> RefundPaymentAsync(RefundPaymentRequest request, RefundPaymentResult result, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request, nameof(request));

            var message = new RefundMessage();

            if (request.IsPartialRefund)
            {
                message.Amount = new MoneyMessage
                {
                    Value = request.AmountToRefund.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                    CurrencyCode = _currencyService.PrimaryCurrency.CurrencyCode
                };
            }

            var refundRequest = new CapturesRefundRequest(request.Order.CaptureTransactionId).WithBody(message);
            var response = await ExecuteRequestAsync(refundRequest, request.Order.StoreId, cancelToken);

            result.NewPaymentStatus = request.IsPartialRefund
                ? PaymentStatus.PartiallyRefunded
                : PaymentStatus.Refunded;

            return response;
        }

        public Task<PayPalResponse> RefundPaymentAsync(CapturesRefundRequest request, CancellationToken cancelToken = default)
            => ExecuteRequestAsync(request, cancelToken);

        /// <summary>
        /// Gets a list of webhooks.
        /// </summary>
        public Task<PayPalResponse> ListWebhooksAsync(ListWebhooksRequest request, CancellationToken cancelToken = default)
            => ExecuteRequestAsync(request, cancelToken);

        /// <summary>
        /// Creates a webhook.
        /// </summary>  
        public Task<PayPalResponse> CreateWebhookAsync(CreateWebhookRequest request, CancellationToken cancelToken = default)
            => ExecuteRequestAsync(request, cancelToken);

        #endregion

        #region Infrastructure

        public Task<PayPalResponse> ExecuteRequestAsync<TRequest>(
            TRequest request,
            CancellationToken cancelToken = default)
            where TRequest : PayPalRequest
        {
            return ExecuteRequestAsync(request, 0, cancelToken);
        }

        public Task<PayPalResponse> ExecuteRequestAsync<TRequest>(
            TRequest request,
            int storeId,
            CancellationToken cancelToken = default)
            where TRequest : PayPalRequest
        {
            return ExecuteRequestAsync(request, _settingFactory.LoadSettings<PayPalSettings>(storeId), cancelToken);
        }

        public virtual async Task<PayPalResponse> ExecuteRequestAsync<TRequest>(
            TRequest request,
            PayPalSettings settings,
            CancellationToken cancelToken = default)
            where TRequest : PayPalRequest
        {
            Guard.NotNull(request, nameof(request));
            Guard.NotNull(settings, nameof(settings));

            request = request.Clone<TRequest>();

            var apiUrl = settings.UseSandbox ? ApiUrlSandbox : ApiUrlLive;
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

            if (request.Headers.Authorization == null || !request.Headers.Authorization.Parameter.HasValue())
            {
                await HandleAuthorizationAsync(request, settings);
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

        /// <summary>
        /// Gets access token and add authorization header.
        /// </summary>
        protected virtual async Task HandleAuthorizationAsync(PayPalRequest request, PayPalSettings settings)
        {
            Guard.NotNull(request, nameof(request));
            Guard.NotNull(settings, nameof(settings));

            if (!request.Headers.Contains("Authorization") && request is not AccessTokenRequest)
            {
                var token = await GetAccessTokenFromCacheAsync(settings);

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            }
        }

        /// <summary>
        /// Gets access token from memory cache or - if not in cache - fetches token from API.
        /// </summary>
        private async Task<AccessToken> GetAccessTokenFromCacheAsync(PayPalSettings settings)
        {
            var cacheKey = string.Format(PAYPAL_ACCESS_TOKEN_KEY, settings.ClientId);
            var memCache = _cacheFactory.GetMemoryCache();
            var token = await memCache.GetAsync(cacheKey, async (o) =>
            {
                var accessTokenRequest = new AccessTokenRequest(settings.ClientId, settings.Secret);
                var response = await ExecuteRequestAsync(accessTokenRequest);
                var accesstoken = response.Body<AccessToken>();

                o.ExpiresIn(TimeSpan.FromSeconds(accesstoken.ExpiresIn - 30));
                o.SetSlidingExpiration(TimeSpan.FromHours(6));

                return accesstoken;
            });

            if (token.IsExpired())
            {
                // Should never happen, but just to be save...
                memCache.Remove(cacheKey);
                return await GetAccessTokenFromCacheAsync(settings);
            }

            return token;
        }

        protected virtual HttpContent SerializeRequest(PayPalRequest request)
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

            if (content == null)
            {
                throw new IOException($"Unable to serialize request with Content-Type {request.ContentType} because it is not supported.");
            }

            return content;
        }

        protected virtual async Task<object> DeserializeResponseAsync(HttpContent content, Type responseType)
        {
            if (content.Headers.ContentType == null)
            {
                throw new IOException("HTTP response did not have content-type header set");
            }

            var contentType = content.Headers.ContentType.ToString().ToLower();

            // ContentType can also be 'application/json; charset=utf-8'
            if (contentType.Contains("application/json"))
            {
                var message = JsonConvert.DeserializeObject(await content.ReadAsStringAsync(), responseType);
                return message;
            }
            else
            {
                throw new IOException($"Unable to deserialize response with Content-Type {contentType} because it is not supported.");
            }
        }

        protected virtual void HandleException(Exception exception, PaymentResult result, bool log = false)
        {
            if (exception != null)
            {
                result.Errors.Add(exception.Message);
                if (log)
                    _logger.Error(exception);
            }
        }

        #endregion

        #region For future use

        /// <summary>
        /// Creates billing plan. (For future use)
        /// </summary>
        //public async Task PrepareRecurringPayment(int storeId, Product product)
        //{
        //    string error = null;
        //    HttpResponseMessage responseMessage = null;

        //    try
        //    {
        //        // TODO: (mh) (core) Create product & store returned product id as GenericAttribute for shop product
        //        // https://developer.paypal.com/api/catalog-products/v1/#products_create

        //        var store = _storeContext.GetStoreById(storeId);
        //        var billingPlanName = _locService.GetResource("TODO.BillingPlanName").FormatWith("TODO:Productname");

        //        var billingPlan = new BillingPlan
        //        {
        //            Name = billingPlanName,
        //            Description = billingPlanName,
        //            Type = "FIXED"  // Smartstore doesn't support infinite cycles
        //        };

        //        var paymentDefinition = new PaymentDefinition
        //        {
        //            Name = billingPlanName,
        //            Cycles = product.RecurringTotalCycles.ToString(),
        //            FrequencyInterval = product.RecurringCyclePeriod.ToString(),
        //            Amount = new MoneyMessage{
        //                // TODO: (mh) (core) Respect discounts & more?
        //                Value = product.Price.ToString("0.00", CultureInfo.InvariantCulture),
        //                CurrencyCode = store.PrimaryStoreCurrency.CurrencyCode
        //            }
        //        };

        //        paymentDefinition.Frequency = product.RecurringCyclePeriod switch
        //        {
        //            RecurringProductCyclePeriod.Days    => "DAY",
        //            RecurringProductCyclePeriod.Weeks   => "WEEK",
        //            RecurringProductCyclePeriod.Months  => "MONTH",
        //            RecurringProductCyclePeriod.Years   => "YEAR",
        //            _ => throw new SmartException("Period not supported."),
        //        };

        //        billingPlan.PaymentDefinitions.Add(paymentDefinition);

        //        var data = JsonConvert.SerializeObject(billingPlan);

        //        await EnsureAuthorizationAsync();
        //        // TODO: (mh) (core) Make request & store plan id as GenericAttribute for shop product
        //        // TODO: (mh) (core) Don't forget to delete Attributes when product properties for Recurrency are changing
        //        //                   or product gets deletet.
        //    }
        //    catch (Exception exception)
        //    {
        //        error = exception.ToString();
        //    }

        //    if (responseMessage != null)
        //    {
        //        if (responseMessage.StatusCode == HttpStatusCode.NoContent || responseMessage.StatusCode == HttpStatusCode.Created)
        //        {
        //            // TODO: (mh) (core) Handle response 
        //        }
        //        else
        //        {
        //            error = responseMessage.ReasonPhrase;
        //        }
        //    }

        //    // TODO: (mh) (core) Handle error
        //}

        #endregion
    }
}