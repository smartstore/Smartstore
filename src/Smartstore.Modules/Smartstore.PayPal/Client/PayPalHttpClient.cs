using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using Smartstore.Caching;
using Smartstore.Core;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.PayPal.Client.Messages;
using Smartstore.PayPal.Services;
using Smartstore.Web.Models.Cart;

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
        private readonly PayPalRequestFactory _requestFactory;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly ICurrencyService _currencyService;
        private readonly IMediaService _mediaService;
        private readonly ICacheFactory _cacheFactory;
        private readonly ISettingFactory _settingFactory;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ITaxService _taxService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductService _productService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IRoundingHelper _roundingHelper;

        public PayPalHttpClient(
            HttpClient client,
            PayPalRequestFactory requestFactory,
            ICheckoutStateAccessor checkoutStateAccessor,
            IStoreContext storeContext,
            IWorkContext workContext,
            ICurrencyService currencyService,
            IMediaService mediaService,
            ICacheFactory cacheFactory,
            ISettingFactory settingFactory,
            IShoppingCartService shoppingCartService,
            ITaxService taxService,
            IPriceCalculationService priceCalculationService,
            IProductService productService,
            IOrderCalculationService orderCalculationService,
            IRoundingHelper roundingHelper)
        {
            _client = client;
            _requestFactory = requestFactory;
            _checkoutStateAccessor = checkoutStateAccessor;
            _storeContext = storeContext;
            _workContext = workContext;
            _currencyService = currencyService;
            _mediaService = mediaService;
            _cacheFactory = cacheFactory;
            _settingFactory = settingFactory;
            _shoppingCartService = shoppingCartService;
            _taxService = taxService;
            _priceCalculationService = priceCalculationService;
            _productService = productService;
            _orderCalculationService = orderCalculationService;
            _roundingHelper = roundingHelper;
        }

        #region Payment processing

        /// <summary>
        /// Activates a plan.
        /// </summary>
        public async Task<PayPalResponse> GetPlanDetailsAsync(string planId, CancellationToken cancelToken = default)
        {
            var request = _requestFactory.PlanGet(planId);
            var response = await ExecuteRequestAsync(request, cancelToken);

            return response;
        }

        /// <summary>
        /// Activates a plan.
        /// </summary>
        public async Task<PayPalResponse> ActivatePlanAsync(string planId, CancellationToken cancelToken = default)
        {
            var request = _requestFactory.PlanActivate(planId);
            var response = await ExecuteRequestAsync(request, cancelToken);

            return response;
        }

        /// <summary>
        /// Deactivates a plan.
        /// </summary>
        public async Task<PayPalResponse> DeactivatePlanAsync(string planId, CancellationToken cancelToken = default)
        {
            var request = _requestFactory.PlanDeactivate(planId);
            var response = await ExecuteRequestAsync(request, cancelToken);

            return response;
        }

        /// <summary>
        /// Creates a subscription.
        /// </summary>
        public async Task<List<PayPalResponse>> CreateSubscriptionsAsync(ProcessPaymentRequest request, CancellationToken cancelToken = default)
        {
            var subscriptionResponses = new List<PayPalResponse>();
            var customer = _workContext.CurrentCustomer;
            var store = _storeContext.GetStoreById(request.StoreId);

            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, request.StoreId);

            //foreach (var item in cart.Items)
            //{
            //    var product = item.Item.Product;
            //    var planId = product.GenericAttributes.Get<string>("PayPalPlanId");
            //    if (planId.HasValue())
            //    { 
            //        var subscription = new Subscription
            //        {
            //            PlanId = planId,
            //            Quantity = item.Item.Quantity.ToString(),
            //            // INFO: We don't need to set this to the current date as it is defaulting to that. We only set this if subscription should start later.
            //            //StartTime = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
            //            Subscriber = new Subscriber
            //            {
            //                // TODO: This must be obtained from a payment info field which must be created.
            //                EmailAddress = "buyer@todo.com"
            //            },
            //        };

            //        var createSubscriptionRequest = new CreateSubscriptionRequest()
            //            .WithRequestId(Guid.NewGuid().ToString())
            //            .WithBody(subscription);

            //        // TODO: Response ain't no Subscription object. Change it and get the links somehow.
            //        // They are important to be placed in the order so the customer is able to confirm his subscription.
            //        var response = await ExecuteRequestAsync(createSubscriptionRequest, cancelToken);
            //        // DEV: uncomment for response viewing 
            //        //var rawResponse = response.Body<object>().ToString();
            //        //dynamic jResponse = JObject.Parse(rawResponse);

            //        subscriptionResponses.Add(response);

            //        // TODO: Store some info 
            //    }
            //    else
            //    {
            //        // TODO: Throw some error.
            //    }
            //}

            return subscriptionResponses;
        }

        /// <summary>
        /// Activates a subscription.
        /// </summary>
        public async Task<PayPalResponse> ActivateSubscriptionsAsync(string subscriptionId, string reason, CancellationToken cancelToken = default)
        {
            var stateChangeMessage = new SubscriptionStateChangeMessage { Reason = reason };
            var activateSubscriptionRequest = _requestFactory.SubscriptionActivate(stateChangeMessage, subscriptionId);

            var response = await ExecuteRequestAsync(activateSubscriptionRequest, cancelToken);
            return response;
        }

        /// <summary>
        /// Cancels a subscription.
        /// </summary>
        public async Task<PayPalResponse> CancelSubscriptionsAsync(string subscriptionId, string reason, CancellationToken cancelToken = default)
        {
            var stateChangeMessage = new SubscriptionStateChangeMessage { Reason = reason };
            var cancelSubscriptionRequest = _requestFactory.SubscriptionActivate(stateChangeMessage, subscriptionId);

            var response = await ExecuteRequestAsync(cancelSubscriptionRequest, cancelToken);
            return response;
        }

        /// <summary>
        /// Suspends a subscription.
        /// </summary>
        public async Task<PayPalResponse> SuspendSubscriptionsAsync(string subscriptionId, string reason, CancellationToken cancelToken = default)
        {
            var stateChangeMessage = new SubscriptionStateChangeMessage { Reason = reason };
            var suspendSubscriptionRequest = _requestFactory.SubscriptionSuspend(stateChangeMessage, subscriptionId);

            var response = await ExecuteRequestAsync(suspendSubscriptionRequest, cancelToken);
            return response;
        }

        /// <summary>
        /// Gets an order.
        /// </summary>
        public async Task<PayPalResponse> GetOrderAsync(string payPalOrderId, CancellationToken cancelToken = default)
        {
            var ordersGetRequest = _requestFactory.OrderGet(payPalOrderId);
            var response = await ExecuteRequestAsync(ordersGetRequest, cancelToken);

            // DEV: uncomment for response viewing 
            //var rawResponse = response.Body<object>().ToString();
            //dynamic jResponse = JObject.Parse(rawResponse);

            return response;
        }

        /// <summary>
        /// Creates a PayPal order for invoice method.
        /// </summary>
        public async Task<PayPalResponse> CreateOrderForInvoiceAsync(ProcessPaymentRequest request, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request);

            var customer = _workContext.CurrentCustomer;
            var store = _storeContext.GetStoreById(request.StoreId);
            var settings = _settingFactory.LoadSettings<PayPalSettings>(request.StoreId);
            var language = _workContext.WorkingLanguage;
            var paymentData = _checkoutStateAccessor.CheckoutState.PaymentData;

            var logoUrl = store.LogoMediaFileId != 0 ? await _mediaService.GetUrlAsync(store.LogoMediaFileId, 0, store.GetBaseUrl(), false) : string.Empty;

            paymentData.TryGetValueAs<string>("PayPalInvoiceBirthdate", out var birthDate);
            paymentData.TryGetValueAs<string>("PayPalInvoicePhoneNumber", out var phoneNumber);

            if (!paymentData.TryGetValueAs<string>("ClientMetaId", out var clientMetaId))
            {
                return null;
            }

            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, request.StoreId);
            var purchaseUnits = await GetPurchaseUnitsAsync(cart, request.OrderTotal, request.OrderGuid.ToString());

            var orderMessage = new OrderMessage
            {
                Intent = Intent.Capture,
                ProcessingInstruction = "ORDER_COMPLETE_ON_PAYMENT_APPROVAL",
                PurchaseUnits = [.. purchaseUnits],
                PaymentSource = new PaymentSource
                {
                    PaymentSourceInvoice = new PaymentSourceInvoice
                    {
                        Name = new NameMessage
                        {
                            GivenName = customer.BillingAddress.FirstName,
                            Surname = customer.BillingAddress.LastName
                        },
                        Email = customer.BillingAddress.Email,
                        BirthDate = birthDate,
                        Phone = new PhoneMessage
                        {
                            NationalNumber = phoneNumber,
                            CountryCode = customer.BillingAddress.Country.DiallingCode.ToString()
                        },
                        BillingAddress = new AddressMessage
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
                            CustomerServiceInstructions =
                            [
                                settings.CustomerServiceInstructions
                            ]
                        }
                    }
                },
                AppContext = new PayPalApplictionContext
                {
                    ShippingPreference = cart.IsShippingRequired ? ShippingPreference.SetProvidedAddress : ShippingPreference.NoShipping
                }
            };

            var orderCreateRequest = _requestFactory.OrderCreate(orderMessage)
                .WithRequestId(Guid.NewGuid().ToString())
                .WithClientMetadataId(clientMetaId.ToString());

            var response = await ExecuteRequestAsync(orderCreateRequest, cancelToken);
            // DEV: uncomment for response viewing 
            //var rawResponse = response.Body<object>().ToString();
            //dynamic jResponse = JObject.Parse(rawResponse);

            return response;
        }

        /// <summary>
        /// Creates a PayPal order for invoice method.
        /// </summary>
        public async Task<PayPalResponse> CreateOrderAsync(OrderMessage orderMessage, CancellationToken cancelToken = default)
        {
            var orderCreateRequest = _requestFactory.OrderCreate(orderMessage).WithRequestId(Guid.NewGuid().ToString());
            var response = await ExecuteRequestAsync(orderCreateRequest, cancelToken);

            return response;
        }

        /// <summary>
        /// Updates an order.
        /// </summary>
        public virtual async Task<PayPalResponse> UpdateOrderAsync(ProcessPaymentRequest request, ProcessPaymentResult result, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request);

            var cart = await _shoppingCartService.GetCartAsync(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, request.StoreId);
            var purchaseUnits = await GetPurchaseUnitsAsync(cart, request.OrderTotal, request.OrderGuid.ToString());
            purchaseUnits[0].ReferenceId = request.OrderGuid.ToString();

            var patches = new List<Patch<object>>
            {
                new() {
                    Op = "replace",
                    Path = "/purchase_units/@reference_id=='default'",
                    Value = purchaseUnits[0]
                }
            };

            var ordersPatchRequest = _requestFactory.OrderPatch(patches, request.PayPalOrderId);
            var response = await ExecuteRequestAsync(ordersPatchRequest, request.StoreId, cancelToken);

            return response;
        }

        /// <summary>
        /// Authorizes an order.
        /// </summary>
        public virtual async Task<PayPalResponse> AuthorizeOrderAsync(ProcessPaymentRequest request, ProcessPaymentResult result, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request);

            var ordersAuthorizeRequest = _requestFactory.OrderAuthorize(request.PayPalOrderId);
            var response = await ExecuteRequestAsync(ordersAuthorizeRequest, request.StoreId, cancelToken);
            var rawResponse = response.Body<object>().ToString();
            dynamic jResponse = JObject.Parse(rawResponse);

            result.AuthorizationTransactionId = (string)jResponse.purchase_units[0].payments.authorizations[0].id;
            result.AuthorizationTransactionCode = (string)jResponse.id;
            result.AuthorizationTransactionResult = (string)jResponse.status;
            result.NewPaymentStatus = PaymentStatus.Authorized;

            return response;
        }

        public Task<PayPalResponse> AuthorizeOrderAsync(string captureId, CancellationToken cancelToken = default)
            => ExecuteRequestAsync(_requestFactory.OrderAuthorize(captureId), cancelToken);

        /// <summary>
        /// Captures an order.
        /// </summary>
        public virtual async Task<PayPalResponse> CaptureOrderAsync(ProcessPaymentRequest request, ProcessPaymentResult result, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request);

            var ordersCaptureRequest = _requestFactory.OrderCapture(request.PayPalOrderId);
            var response = await ExecuteRequestAsync(ordersCaptureRequest, request.StoreId, cancelToken);
            var rawResponse = response.Body<object>().ToString();

            dynamic jResponse = JObject.Parse(rawResponse);

            result.CaptureTransactionId = (string)jResponse.purchase_units[0].payments.captures[0].id;
            result.AuthorizationTransactionCode = (string)jResponse.id;
            result.CaptureTransactionResult = (string)jResponse.status;
            result.NewPaymentStatus = PaymentStatus.Paid;

            return response;
        }

        public Task<PayPalResponse> CaptureOrderAsync(string orderId, CancellationToken cancelToken = default)
            => ExecuteRequestAsync(_requestFactory.OrderCapture(orderId), cancelToken);

        /// <summary>
        /// Captures authorized payment.
        /// </summary>
        public virtual async Task<PayPalResponse> CapturePaymentAsync(CapturePaymentRequest request, CapturePaymentResult result, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request);

            // TODO: (mh) (core) If ERPs are used this ain't the real Invoice-Id > Make optional or remove (TBD with MC)
            var message = new CaptureMessage { InvoiceId = request.Order.OrderNumber };
            var voidRequest = _requestFactory.AuthorizationCapture(message, request.Order.AuthorizationTransactionId);
            var response = await ExecuteRequestAsync(voidRequest, request.Order.StoreId, cancelToken);
            var capture = response.Body<CaptureMessage>();

            result.NewPaymentStatus = PaymentStatus.Paid;
            result.CaptureTransactionId = capture.Id;
            result.CaptureTransactionResult = capture.Status;

            return response;
        }

        /// <summary>
        /// Voids authorized payment.
        /// </summary>
        public virtual async Task<PayPalResponse> VoidPaymentAsync(VoidPaymentRequest request, VoidPaymentResult result, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request);

            var voidRequest = _requestFactory.AuthorizationVoid(request.Order.AuthorizationTransactionId);
            var response = await ExecuteRequestAsync(voidRequest, request.Order.StoreId, cancelToken);

            result.NewPaymentStatus = PaymentStatus.Voided;

            return response;
        }

        /// <summary>
        /// Refunds captured payment.
        /// </summary>
        public virtual async Task<PayPalResponse> RefundPaymentAsync(RefundPaymentRequest request, RefundPaymentResult result, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request);

            var message = new RefundMessage();

            if (request.IsPartialRefund)
            {
                message.Amount = new MoneyMessage
                {
                    Value = request.AmountToRefund.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                    CurrencyCode = _currencyService.PrimaryCurrency.CurrencyCode
                };
            }

            var refundRequest = _requestFactory.CaptureRefund(message, request.Order.CaptureTransactionId);
            var response = await ExecuteRequestAsync(refundRequest, request.Order.StoreId, cancelToken);

            result.NewPaymentStatus = request.IsPartialRefund
                ? PaymentStatus.PartiallyRefunded
                : PaymentStatus.Refunded;

            return response;
        }

        /// <summary>
        /// Gets a list of webhooks.
        /// </summary>
        public Task<PayPalResponse> ListWebhooksAsync(CancellationToken cancelToken = default)
            => ExecuteRequestAsync(_requestFactory.WebhooksList(), cancelToken);

        /// <summary>
        /// Creates a webhook.
        /// </summary>  
        public Task<PayPalResponse> CreateWebhookAsync(Webhook webhook, CancellationToken cancelToken = default)
            => ExecuteRequestAsync(_requestFactory.WebhookCreate(webhook), cancelToken);

        /// <summary>
        /// Adds a tracking number to a PayPal order.
        /// </summary>
        public virtual async Task<PayPalResponse> AddTrackingNumberAsync(Shipment shipment, CancellationToken cancelToken = default)
        {
            Guard.NotNull(shipment);

            var trackingMessage = new TrackingMessage
            {
                Carrier = "OTHER",
                CarrierNameOther = shipment.Order.ShippingMethod,
                TrackingNumber = shipment.TrackingNumber,
                CaptureId = shipment.Order.CaptureTransactionId
            };

            var shipmentItems = new List<Messages.ShipmentItem>();
            foreach (var item in shipment.ShipmentItems)
            {
                var orderItem = shipment.Order.OrderItems.Where(x => x.Id == item.OrderItemId).FirstOrDefault();

                shipmentItems.Add(new Messages.ShipmentItem
                {
                    Sku = orderItem.Sku,
                    Quantity = item.Quantity.ToString(),
                    Name = orderItem.Product.Name
                });
            }

            trackingMessage.Items = [.. shipmentItems];

            var trackingRequest = _requestFactory.OrderAddTracking(trackingMessage, shipment.Order.AuthorizationTransactionCode.ToString())
                .WithRequestId(Guid.NewGuid().ToString());

            var response = await ExecuteRequestAsync(trackingRequest, shipment.Order.StoreId, cancelToken);

            return response;
        }

        /// <summary>
        /// Updates a tracking number for a PayPal order.
        /// </summary>
        public virtual async Task<PayPalResponse> CancelTrackingNumberAsync(Shipment shipment, CancellationToken cancelToken = default)
        {
            Guard.NotNull(shipment);
            var trackingId = shipment.GenericAttributes.Get<string>("PayPalTrackingId");

            // If no tracking id is stored it means the previous number wasn't transmitted to PayPal yet thus we can't cancel the tracking number.
            if (!trackingId.HasValue())
            {
                return null;
            }

            var patches = new List<Patch<string>>
            {
                new() {
                    Op = "replace",
                    Path = "/status",
                    Value = "CANCELLED"
                }
            };

            var updateTrackingRequest = _requestFactory.OrderUpdateTracking(patches, shipment.Order.AuthorizationTransactionCode.ToString(), trackingId);
            var response = await ExecuteRequestAsync(updateTrackingRequest, shipment.Order.StoreId, cancelToken);

            return response;
        }

        /// <summary>
        /// Creates a product
        /// </summary>
        public async Task<PayPalResponse> CreateProductAsync(Product product, CancellationToken cancelToken = default)
        {
            var imageUrl = string.Empty;

            // TODO: Which is the current store context?
            var store = _storeContext.CurrentStore;
            if (product.MainPictureId != null)
            {
                var file = await _mediaService.GetFileByIdAsync((int)product.MainPictureId);
                imageUrl = _mediaService.GetUrl(file, 0, store.GetBaseUrl());
            }   
            
            var productMessage = new ProductMessage 
            {
                // Ensure Id is not empty & has at least 6 characters
                Id = (product.Sku.HasValue() ? product.Sku : product.Id.ToString()).PadLeft(6, '*'), 
                Name = product.GetLocalized(x => x.Name),
                Description = product.GetLocalized(x => x.ShortDescription).Value.Truncate(256, "..."),
                // TODO: Init according to product type
                Type = PayPalProductType.Physical,
                ImageUrl = imageUrl,
                HomeUrl = $"{store.GetBaseUrl()}{await product.GetActiveSlugAsync()}"
            };

            var createProductRequest = _requestFactory.ProductCreate(productMessage);
            var response = await ExecuteRequestAsync(createProductRequest, cancelToken);

            return response;
        }

        /// <summary>
        /// Creates a plan
        /// </summary>
        public async Task<PayPalResponse> CreatePlanAsync(Plan plan, CancellationToken cancelToken = default)
        {
            var createPlanRequest = _requestFactory.PlanCreate(plan);
            var response = await ExecuteRequestAsync(createPlanRequest, cancelToken);

            return response;
        }

        #endregion

        #region Utilities 

        private async Task<List<PurchaseUnitItem>> GetPurchaseUnitItemsAsync(ShoppingCart cart, Customer customer, Currency currency)
        {
            var model = await cart.MapAsync(isEditable: false, prepareEstimateShippingIfEnabled: false);
            var purchaseUnitItems = new List<PurchaseUnitItem>();
            var cartProducts = cart.Items.Select(x => x.Item.Product).ToArray();
            var batchContext = _productService.CreateProductBatchContext(cartProducts, null, customer, false);
            var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, customer, _currencyService.PrimaryCurrency, batchContext);

            var isVatExempt = await _taxService.IsVatExemptAsync(customer);

            foreach (var item in model.Items)
            {
                var cartItem = cart.Items.Where(x => x.Item.ProductId == item.ProductId).FirstOrDefault();
                var taxRate = await _taxService.GetTaxRateAsync(cartItem.Item.Product);
                var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(cartItem, calculationOptions);
                var (unitPrice, subtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);
                
                var convertedUnitPriceNet = _currencyService.ConvertToWorkingCurrency(unitPrice.Tax.Value.PriceNet);
                var convertedUnitPriceTax = _currencyService.ConvertToWorkingCurrency(unitPrice.Tax.Value.Amount);

                var productName = item.ProductName?.Value?.Truncate(126);
                var productDescription = item.ShortDesc?.Value?.Truncate(126);

                var purchaseUnitItem = new PurchaseUnitItem
                {
                    UnitAmount = new MoneyMessage
                    {
                        Value = convertedUnitPriceNet.Amount.ToStringInvariant("F"),
                        CurrencyCode = currency.CurrencyCode
                    },
                    Name = productName,
                    Description = productDescription,
                    Category = item.IsEsd ? ItemCategoryType.DigitalGoods : ItemCategoryType.PhysicalGoods,
                    Quantity = item.EnteredQuantity.ToString(),
                    Sku = item.Sku
                };

                if (!isVatExempt)
                {
                    purchaseUnitItem.Tax = new MoneyMessage
                    {
                        Value = convertedUnitPriceTax.Amount.ToStringInvariant("F"),
                        CurrencyCode = currency.CurrencyCode
                    };
                    purchaseUnitItem.TaxRate = taxRate.Rate.ToStringInvariant("F");
                }

                purchaseUnitItems.Add(purchaseUnitItem);
            }

            return purchaseUnitItems;
        }

        private async Task<List<PurchaseUnit>> GetPurchaseUnitsAsync(
            ShoppingCart cart,
            decimal orderTotal = 0,
            string orderGuid = "")
        {
            var customer = _workContext.CurrentCustomer;
            var currency = _workContext.WorkingCurrency;

            var purchaseUnitItems = await GetPurchaseUnitItemsAsync(cart, customer, currency);
            var isVatExempt = await _taxService.IsVatExemptAsync(customer);

            // Get subtotal
            var cartSubTotalExclTax = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart, false);
            var cartSubTotalInklTax = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart, true);
            var subTotalConverted = _currencyService.ConvertFromPrimaryCurrency(cartSubTotalExclTax.SubtotalWithoutDiscount.Amount, currency);

            // Get tax
            (Money price, _) = await _orderCalculationService.GetShoppingCartTaxTotalAsync(cart);
            var cartTax = _currencyService.ConvertFromPrimaryCurrency(price.Amount, currency);

            var amountValue = orderTotal == 0
                ? (subTotalConverted.Amount + cartTax.Amount).ToStringInvariant("F")
                : orderTotal.ToStringInvariant("F");

            var format = new NumberFormatInfo { NumberDecimalSeparator = "." };

            decimal itemTotal = 0;
            purchaseUnitItems.Each(x => itemTotal += x.UnitAmount.Value.Convert<decimal>() * x.Quantity.ToInt());

            decimal itemTotalTax = 0;
            purchaseUnitItems.Each(x => itemTotalTax += x.Tax != null ? x.Tax.Value.Convert<decimal>() * x.Quantity.ToInt() : 0);

            var purchaseUnit = new PurchaseUnit
            {
                Amount = new AmountWithBreakdown
                {
                    Value = amountValue,
                    CurrencyCode = currency.CurrencyCode,
                    AmountBreakdown = new AmountBreakdown
                    {
                        ItemTotal = new MoneyMessage
                        {
                            //Value = subTotalConverted.Amount.ToStringInvariant("F"),
                            Value = itemTotal.ToStringInvariant("F"),
                            CurrencyCode = currency.CurrencyCode
                        },
                        TaxTotal = new MoneyMessage
                        {
                            //Value = cartTax.Amount.ToStringInvariant("F"),
                            Value = itemTotalTax.ToStringInvariant("F"),
                            CurrencyCode = currency.CurrencyCode
                        }
                    }
                },
                Description = string.Empty,
                Items = [.. purchaseUnitItems]
            };

            // INFO: We must execute the following code also for cart pages in case of customer backward navigation,
            // where shipping method might be set or discounts might be applied
            
            var cartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart);

            // Discount
            var orderTotalDiscountAmount = new Money();
            if (cartTotal.DiscountAmount > decimal.Zero)
            {
                orderTotalDiscountAmount = _currencyService.ConvertFromPrimaryCurrency(cartTotal.DiscountAmount.Amount, currency);
            }

            var subTotalDiscountAmount = new Money();
            if (cartSubTotalInklTax.DiscountAmount > decimal.Zero)
            {
                subTotalDiscountAmount = _currencyService.ConvertFromPrimaryCurrency(
                    isVatExempt ? cartSubTotalExclTax.DiscountAmount.Amount : cartSubTotalInklTax.DiscountAmount.Amount, 
                    currency);
            }

            decimal discountAmount = _roundingHelper.Round(orderTotalDiscountAmount.Amount + subTotalDiscountAmount.Amount);
            
            purchaseUnit.Amount.AmountBreakdown.Discount = new MoneyMessage
            {
                Value = discountAmount.ToStringInvariant("F"),
                CurrencyCode = currency.CurrencyCode
            };

            // Get shipping cost
            var shippingTotal = await _orderCalculationService.GetShoppingCartShippingTotalAsync(cart, !isVatExempt);
            var shippingTotalAmount = new Money();
            if (shippingTotal.ShippingTotal != null)
            {
                shippingTotalAmount = _currencyService.ConvertFromPrimaryCurrency(_roundingHelper.Round(shippingTotal.ShippingTotal.Value.Amount), currency);
                purchaseUnit.Amount.AmountBreakdown.Shipping = new MoneyMessage
                {
                    Value = shippingTotalAmount.Amount.ToStringInvariant("F"),
                    CurrencyCode = currency.CurrencyCode
                };
            }

            if (cartTotal.Total != null && cartTotal.Total.Value != cartSubTotalExclTax.SubtotalWithDiscount)
            {
                var cartTotalConverted = _currencyService.ConvertFromPrimaryCurrency(cartTotal.Total.Value.Amount, currency);
                purchaseUnit.Amount.Value = cartTotalConverted.Amount.ToStringInvariant("F");
            }

            // TODO: (mh) (core) This is very hackish. PayPal was contacted and requested for a correct solution.
            // Lets check for rounding issues.
            var calculatedAmount = itemTotal + itemTotalTax + shippingTotalAmount.Amount - discountAmount;
            var amountMismatch = calculatedAmount != purchaseUnit.Amount.Value.Convert<decimal>();
            if (amountMismatch)
            {
                var difference = purchaseUnit.Amount.Value.Convert<decimal>() - calculatedAmount;
                if (difference > 0)
                {
                    purchaseUnit.Amount.AmountBreakdown.Handling = new MoneyMessage
                    {
                        Value = difference.ToStringInvariant("F"),
                        CurrencyCode = currency.CurrencyCode
                    };
                }
                else
                {
                    // Negative mismatch 
                    purchaseUnit.Amount.AmountBreakdown.Discount.Value = (discountAmount + (difference * -1)).ToStringInvariant("F");
                }
            }

            purchaseUnit.CustomId = orderGuid;

            if (customer.ShippingAddress != null)
            {
                purchaseUnit.Shipping = new ShippingDetail
                {
                    ShippingName = new ShippingName
                    {
                        FullName = customer.ShippingAddress.GetFullName()
                    },
                    ShippingAddress = new ShippingAddress
                    {
                        AddressLine1 = customer.ShippingAddress.Address1,
                        AddressLine2 = customer.ShippingAddress.Address2,
                        AdminArea1 = customer.ShippingAddress.StateProvince?.Name,
                        AdminArea2 = customer.ShippingAddress.City,
                        PostalCode = customer.ShippingAddress.ZipPostalCode,
                        CountryCode = customer.ShippingAddress.Country?.TwoLetterIsoCode
                    }
                };
            }
            
            return [purchaseUnit];
        }

        public async Task<OrderMessage> GetOrderForStandardProviderAsync(string orderGuid = "", bool isExpressCheckout = true, bool isApm = false)
        {
            var cart = await _shoppingCartService.GetCartAsync(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);
            var settings = _settingFactory.LoadSettings<PayPalSettings>(_storeContext.CurrentStore.Id);
            var purchaseUnits = await GetPurchaseUnitsAsync(cart, orderGuid: orderGuid);

            var orderMessage = new OrderMessage
            {
                Intent = settings.Intent == PayPalTransactionType.Capture ? Intent.Capture : Intent.Authorize,
                PurchaseUnits = [.. purchaseUnits],
                AppContext = new PayPalApplictionContext
                {
                    ShippingPreference = isExpressCheckout
                        ? ShippingPreference.GetFromFile
                        : cart.IsShippingRequired ? ShippingPreference.SetProvidedAddress : ShippingPreference.NoShipping
                }
            };

            // APMs only support direct capturing
            if (isApm)
            {
                orderMessage.Intent = Intent.Capture;
                orderMessage.ProcessingInstruction = "ORDER_COMPLETE_ON_PAYMENT_APPROVAL";
            }
            
            return orderMessage;
        }

        #endregion

        #region Infrastructure

        public PayPalRequestFactory RequestFactory => _requestFactory;

        public Task<PayPalResponse> ExecuteRequestAsync<TRequest>(
            TRequest request,
            CancellationToken cancelToken = default)
            where TRequest : PayPalRequest
        {
            return ExecuteRequestAsync(request, _storeContext.CurrentStore.Id, cancelToken);
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
            Guard.NotNull(request);
            Guard.NotNull(settings);

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

            // Identifier for PayPal. Please don't change to correct name Smartstore (with the second s small). It's depositied at PayPal and they can't change it.
            request.Headers.Add("PayPal-Partner-Attribution-Id", "SmartStore_Cart_PPCP");

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
            Guard.NotNull(request);
            Guard.NotNull(settings);

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
                var accessTokenRequest = _requestFactory.AccessToken(settings.ClientId, settings.Secret);
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
                throw new PayPalException("HttpRequest did not have content-type header set");
            }

            request.ContentType = request.ContentType.ToLower();

            HttpContent content = null;

            if (request.ContentType == "application/json")
            {
                var json = JsonConvert.SerializeObject(request.Body, PayPalHelper.SerializerSettings);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            else if (request.ContentType == "application/x-www-form-urlencoded")
            {
                content = new FormUrlEncodedContent((IEnumerable<KeyValuePair<string, string>>)request.Body);
            }

            if (content == null)
            {
                throw new PayPalException($"Unable to serialize request with Content-Type {request.ContentType} because it is not supported.");
            }

            return content;
        }

        protected virtual async Task<object> DeserializeResponseAsync(HttpContent content, Type responseType)
        {
            if (content.Headers.ContentType == null)
            {
                throw new PayPalException("HTTP response did not have content-type header set");
            }

            var contentType = content.Headers.ContentType.ToString().ToLower();

            // ContentType can also be 'application/json; charset=utf-8'
            if (contentType.Contains("application/json"))
            {
                var contentString = await content.ReadAsStringAsync();
                var message = JsonConvert.DeserializeObject(contentString, responseType, PayPalHelper.SerializerSettings);
                return message;
            }
            else
            {
                throw new PayPalException($"Unable to deserialize response with Content-Type {contentType} because it is not supported.");
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
