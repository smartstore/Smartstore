using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;
using Smartstore.PayPal.Client;
using Smartstore.PayPal.Client.Messages;
using Smartstore.PayPal.Services;
using Smartstore.Utilities.Html;
using Smartstore.Web.Controllers;
using Smartstore.Web.Models.Cart;

namespace Smartstore.PayPal.Controllers
{
    public class PayPalController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly ICheckoutWorkflow _checkoutWorkflow;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IRoundingHelper _roundingHelper;
        private readonly PayPalHttpClient _client;
        private readonly PayPalSettings _settings;
        private readonly Currency _primaryCurrency;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductService _productService;
        private readonly ICurrencyService _currencyService;
        private readonly ITaxService _taxService;
        private readonly IOrderCalculationService _orderCalculationService;

        public PayPalController(
            SmartDbContext db,
            ICheckoutStateAccessor checkoutStateAccessor,
            ICheckoutWorkflow checkoutWorkflow,
            IShoppingCartService shoppingCartService,
            IOrderProcessingService orderProcessingService,
            IRoundingHelper roundingHelper,
            PayPalHttpClient client,
            PayPalSettings settings,
            ICurrencyService currencyService,
            IPriceCalculationService priceCalculationService,
            IProductService productService,
            ITaxService taxService,
            IOrderCalculationService orderCalculationService)
        {
            _db = db;
            _checkoutStateAccessor = checkoutStateAccessor;
            _checkoutWorkflow = checkoutWorkflow;
            _shoppingCartService = shoppingCartService;
            _orderProcessingService = orderProcessingService;
            _roundingHelper = roundingHelper;
            _client = client;
            _settings = settings;
            _priceCalculationService = priceCalculationService;
            _productService = productService;
            _currencyService = currencyService;
            _taxService = taxService;
            _orderCalculationService = orderCalculationService;

            // INFO: Services wasn't resolved anymore in ctor.
            //_primaryCurrency = Services.CurrencyService.PrimaryCurrency;
            _primaryCurrency = currencyService.PrimaryCurrency;
        }

        [HttpPost]
        public async Task<IActionResult> InitTransaction(string orderId, string routeIdent)
        {
            var success = false;
            var message = string.Empty;

            if (!orderId.HasValue())
            {
                return Json(new { success, message = "No order id has been returned by PayPal." });
            }

            var customer = Services.WorkContext.CurrentCustomer;
            var checkoutState = _checkoutStateAccessor.CheckoutState;

            // Remove unwanted custom properties that might be left from last checkout.
            checkoutState.CustomProperties.Remove("PayPalPayerActionRequired");
            checkoutState.CustomProperties.Remove("UpdatePayPalOrder");
            
            // Only set this if we're not on payment page.
            if (routeIdent != "Checkout.PaymentMethod")
            {
                checkoutState.CustomProperties["PayPalButtonUsed"] = true;
            }

            // Store order id temporarily in checkout state.
            checkoutState.CustomProperties["PayPalOrderId"] = orderId;

            var paypalCheckoutState = checkoutState.GetCustomState<PayPalCheckoutState>();
            paypalCheckoutState.PayPalOrderId = orderId;

            var session = HttpContext.Session;

            if (!session.TryGetObject<ProcessPaymentRequest>("OrderPaymentInfo", out var processPaymentRequest) || processPaymentRequest == null)
            {
                processPaymentRequest = new ProcessPaymentRequest
                {
                    OrderGuid = Guid.NewGuid()
                };
            }

            processPaymentRequest.PayPalOrderId = orderId;
            processPaymentRequest.StoreId = Services.StoreContext.CurrentStore.Id;
            processPaymentRequest.CustomerId = customer.Id;
            processPaymentRequest.PaymentMethodSystemName = customer.GenericAttributes.SelectedPaymentMethod;

            session.TrySetObject("OrderPaymentInfo", processPaymentRequest);

            if (customer.BillingAddress == null && _settings.UseTransmittedAddresses)
            {
                // If adding shipping address fails, just log it and continue.
                try
                {
                    await AddAddressesAsync(orderId);
                }
                catch (Exception ex)
                {
                    Logger.Info("Adding of shipping address has failed.", ex);
                }
            }

            // Get redirect URL if quick checkout is active.
            var redirectUrl = string.Empty;
            var cart = await _shoppingCartService.GetCartAsync(storeId: Services.StoreContext.CurrentStore.Id);
            var result = await _checkoutWorkflow.AdvanceAsync(new(cart, HttpContext, Url));
            if (result.ActionResult != null)
            {
                var redirectToAction = (RedirectToActionResult)result.ActionResult;
                redirectUrl = Url.Action(redirectToAction.ActionName, redirectToAction.ControllerName, redirectToAction.RouteValues, Request.Scheme);
            }

            success = true;

            return Json(new { success, message, redirectUrl });
        }

        /// <summary>
        /// AJAX
        /// Creates a PayPal order VIA API Request and returns the order id.
        /// </summary>
        /// <param name="query">
        /// Needed to validate and thus save the cart before the order is created. 
        /// If this wouldn't have been done the cart value might change 
        /// because the current user data entered (checkout attrs, reward points, etc.) on cart page might not have been saved.
        /// </param>
        /// <param name="useRewardPoints">Needed to validate and thus save the cart before the order is created. </param>
        /// <param name="paymentSource">The current payment source.</param>
        /// <param name="routeIdent">The current route identifier.</param>
        /// <returns>The PayPal order object.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateOrder(ProductVariantQuery query, bool? useRewardPoints, string paymentSource, string routeIdent = "")
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var store = Services.StoreContext.CurrentStore;

            // Only save cart data when we're on shopping cart page.
            if (routeIdent == "ShoppingCart.Cart")
            {
                var warnings = new List<string>();
                var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
                var isCartValid = await _shoppingCartService.SaveCartDataAsync(cart, warnings, query, useRewardPoints, false);

                if (!isCartValid)
                {
                    return Json(new { success = false, message = string.Join(Environment.NewLine, warnings) });
                }
            }

            var session = HttpContext.Session;

            if (!session.TryGetObject<ProcessPaymentRequest>("OrderPaymentInfo", out var processPaymentRequest) || processPaymentRequest == null)
            {
                processPaymentRequest = new ProcessPaymentRequest
                {
                    OrderGuid = Guid.NewGuid()
                };
            }

            session.TrySetObject("OrderPaymentInfo", processPaymentRequest);

            var selectedPaymentMethod = string.Empty;
            switch (paymentSource)
            {
                case "paypal-creditcard-hosted-fields-container":
                    selectedPaymentMethod = PayPalConstants.CreditCard;
                    break;
                case "paypal-sepa-button-container":
                    selectedPaymentMethod = PayPalConstants.Sepa;
                    break;
                case "paypal-paylater-button-container":
                    selectedPaymentMethod = PayPalConstants.PayLater;
                    break;
                case "paypal-google-pay-container":
                    selectedPaymentMethod = PayPalConstants.GooglePay;
                    break;
                case "paypal-button-container":
                default:
                    selectedPaymentMethod = PayPalConstants.Standard;
                    break;
            }

            customer.GenericAttributes.SelectedPaymentMethod = selectedPaymentMethod;
            await customer.GenericAttributes.SaveChangesAsync();

            var orderMessage = await _client.GetOrderForStandardProviderAsync(processPaymentRequest.OrderGuid.ToString(), isExpressCheckout: true);

            if (orderMessage.PurchaseUnits[0].Amount.Value.Convert<decimal>() <= 0)
            {
                return Json(new { success = false, message = T("Plugins.Smartstore.PayPal.Error.CannotBeZeroOrNegative") });
            }

            orderMessage.AppContext.ReturnUrl = store.GetAbsoluteUrl(Url.Action(nameof(RedirectionSuccess), "PayPal"));
            orderMessage.AppContext.CancelUrl = store.GetAbsoluteUrl(Url.Action(nameof(RedirectionCancel), "PayPal"));

            var orderMessagePaymentSource = new PaymentSource();

            if (selectedPaymentMethod == PayPalConstants.GooglePay)
            {
                orderMessagePaymentSource.PaymentSourceGooglePay = new PaymentSourceGooglePay
                {
                    Attributes = new PayPalAttributes
                    {
                        Verification = new VerificationAttribute
                        {
                            Method = "SCA_ALWAYS"
                        }
                    }
                };
            }
            else
            {
                orderMessagePaymentSource.PaymentSourceWallet = new PaymentSourceWallet
                {
                    ReturnUrl = orderMessage.AppContext.ReturnUrl,
                    CancelUrl = orderMessage.AppContext.CancelUrl
                };
            }

            orderMessage.PaymentSource = orderMessagePaymentSource;

            var response = await _client.CreateOrderAsync(orderMessage);
            var rawResponse = response.Body<object>().ToString();
            dynamic jResponse = JObject.Parse(rawResponse);

            return Json(new { success = true, data = jResponse });
        }

        private async Task AddAddressesAsync(string payPalOrderId) 
        {
            var getOrderResponse = await _client.GetOrderAsync(payPalOrderId);
            var order = getOrderResponse.Body<OrderMessage>();

            var shippingAddress = order.PurchaseUnits[0].Shipping?.ShippingAddress;
            var shippingName = order.PurchaseUnits[0].Shipping?.ShippingName?.FullName;

            if (shippingAddress == null || shippingName == null) return;

            var customer = Services.WorkContext.CurrentCustomer;

            var preferredBillingAddressFirstname = order.Payer.Name.GivenName;
            var preferredBillingAddressLastname = order.Payer.Name.Surname;

            var nameParts = SplitFullName(shippingName);

            var country = await _db.Countries
                .Where(x => x.TwoLetterIsoCode == shippingAddress.CountryCode)
                .FirstOrDefaultAsync();

            var stateProvince = country != null
                ? await _db.StateProvinces
                    .Where(x => x.CountryId == country.Id && x.Abbreviation == shippingAddress.AdminArea1)
                    .FirstOrDefaultAsync()
                : null;

            var address = new Address
            {
                Email = order.Payer?.EmailAddress,
                Address1 = shippingAddress.AddressLine1,
                Address2 = shippingAddress.AddressLine2,
                City = shippingAddress.AdminArea2,
                ZipPostalCode = shippingAddress.PostalCode,
                CountryId = country?.Id,
                StateProvinceId = stateProvince?.Id,

                // INFO: Use the payer name for billing as it reflects the buyer's primary identity,
                // and use the shipping address name for delivery, if specified, to account for possible different recipients.
                FirstName = preferredBillingAddressFirstname.HasValue() ? preferredBillingAddressFirstname : nameParts.FirstName,
                LastName = preferredBillingAddressLastname.HasValue() ? preferredBillingAddressLastname : nameParts.LastName
            };

            // Add billing address if it doesn't exist yet.
            if (customer.Addresses.FindAddress(address) == null)
            {
                customer.Addresses.Add(address);
            }

            customer.BillingAddress = address;

            // Add shipping address if it doesn't exist yet.
            address.FirstName = nameParts.FirstName;
            address.LastName = nameParts.LastName;
            
            if (customer.Addresses.FindAddress(address) == null)
            {
                customer.Addresses.Add(address);
            }

            customer.ShippingAddress = address;

            await _db.SaveChangesAsync();
        }

        private static (string FirstName, string LastName) SplitFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return (string.Empty, string.Empty);
            }

            var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (nameParts.Length == 1)
            {
                return (nameParts[0], string.Empty);
            }

            var firstName = string.Join(' ', nameParts.Take(nameParts.Length - 1));
            var lastName = nameParts[^1];

            return (firstName, lastName);
        }

        /// <summary>
        /// Called by ajax from credit card hosted fields to get two letter country code by id.
        /// </summary>
        /// <param name="id">The id of the selected country</param>
        /// <returns>ISO Code of the selected country</returns>
        [HttpPost]
        public async Task<IActionResult> GetCountryCodeById(int countryId)
        {
            var country = await _db.Countries.FindByIdAsync(countryId, false);
            var code = country?.TwoLetterIsoCode;

            return Json(code);
        }

        /// <summary>
        /// AJAX
        /// Called after buyer clicked buy-now-button but before the order was created.
        /// Processes payment and return redirect URL if there is any.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(string formData)
        {
            string redirectUrl = null;
            var messages = new List<string>();
            var success = false;

            try
            {
                var store = Services.StoreContext.CurrentStore;
                var customer = Services.WorkContext.CurrentCustomer;

                if (!HttpContext.Session.TryGetObject<ProcessPaymentRequest>("OrderPaymentInfo", out var paymentRequest) || paymentRequest == null)
                {
                    paymentRequest = new ProcessPaymentRequest();
                }

                var state = _checkoutStateAccessor.CheckoutState.GetCustomState<PayPalCheckoutState>();

                if (state.ApmProviderSystemName.HasValue())
                {
                    try
                    {
                        await CreateOrderApmAsync(paymentRequest.OrderGuid.ToString());
                    } 
                    catch (PayPalException ex)
                    {
                        PayPalHelper.HandleException(ex, T);
                    }
                    
                    paymentRequest.PaymentMethodSystemName = state.ApmProviderSystemName;
                }
                
                paymentRequest.StoreId = store.Id;
                paymentRequest.CustomerId = customer.Id;
                
                // We must check here if an order can be placed to avoid creating unauthorized transactions.
                var (warnings, cart) = await _orderProcessingService.ValidateOrderPlacementAsync(paymentRequest);
                if (warnings.Count == 0)
                {
                    if (await _orderProcessingService.IsMinimumOrderPlacementIntervalValidAsync(customer, store))
                    {
                        success = true;
                        state.IsConfirmed = true;
                        state.FormData = formData.EmptyNull();

                        paymentRequest.PayPalOrderId = state.PayPalOrderId;

                        HttpContext.Session.TrySetObject("OrderPaymentInfo", paymentRequest);

                        redirectUrl = state.ApmRedirectActionUrl;
                    }
                    else
                    {
                        messages.Add(T("Checkout.MinOrderPlacementInterval"));
                    }
                }
                else
                {
                    messages.AddRange(warnings.Select(HtmlUtility.ConvertPlainTextToHtml));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                messages.Add(ex.Message);
            }

            return Json(new { success, redirectUrl, messages });
        }

        private async Task CreateOrderApmAsync(string orderGuid)
        {
            var orderMessage = await _client.GetOrderForStandardProviderAsync(orderGuid, true, true);
            var checkoutState = _checkoutStateAccessor.CheckoutState.GetCustomState<PayPalCheckoutState>();

            // Get values from checkout input fields which were saved in CheckoutState.
            orderMessage.PaymentSource = GetPaymentSource(checkoutState);

            orderMessage.AppContext.Locale = Services.WorkContext.WorkingLanguage.LanguageCulture;

            // Get ReturnUrl & CancelUrl and add them to PayPalApplicationContext for proper redirection after payment was made or cancelled.
            var store = Services.StoreContext.CurrentStore;
            orderMessage.AppContext.ReturnUrl = store.GetAbsoluteUrl(Url.Action(nameof(RedirectionSuccess), "PayPal"));
            orderMessage.AppContext.CancelUrl = store.GetAbsoluteUrl(Url.Action(nameof(RedirectionCancel), "PayPal"));

            var response = await _client.CreateOrderAsync(orderMessage);
            var rawResponse = response.Body<object>().ToString();
            dynamic jResponse = JObject.Parse(rawResponse);

            // Save redirect url in CheckoutState.
            var status = (string)jResponse.status;
            checkoutState.PayPalOrderId = (string)jResponse.id;
            if (status == "PAYER_ACTION_REQUIRED")
            {
                var link = ((JObject)jResponse).SelectToken("links")
                            .Where(t => t["rel"].Value<string>() == "payer-action")
                            .First()["href"].Value<string>();

                checkoutState.ApmRedirectActionUrl = link;
            }
        }

        private PaymentSource GetPaymentSource(PayPalCheckoutState checkoutState)
        {
            var apmPaymentSource = new PaymentSourceApm
            {
                CountryCode = checkoutState.ApmCountryCode,
                Name = checkoutState.ApmFullname
            };

            var paymentSource = new PaymentSource();

            switch (checkoutState.ApmProviderSystemName)
            {
                case PayPalConstants.Trustly:
                    paymentSource.PaymentSourceTrustly = apmPaymentSource;
                    break;
                case PayPalConstants.Bancontact:
                    paymentSource.PaymentSourceBancontact = apmPaymentSource;
                    break;
                case PayPalConstants.Blik:
                    paymentSource.PaymentSourceBlik = apmPaymentSource;
                    break;
                case PayPalConstants.Eps:
                    paymentSource.PaymentSourceEps = apmPaymentSource;
                    break;
                case PayPalConstants.Ideal:
                    paymentSource.PaymentSourceIdeal = apmPaymentSource;
                    break;
                case PayPalConstants.MyBank:
                    paymentSource.PaymentSourceMyBank = apmPaymentSource;
                    break;
                case PayPalConstants.Przelewy24:
                    paymentSource.PaymentSourceP24 = apmPaymentSource;
                    apmPaymentSource.Email = checkoutState.ApmEmail;
                    break;
                default:
                    break;
            }

            return paymentSource;
        }

        public IActionResult RedirectionSuccess()
        {
            var state = _checkoutStateAccessor.CheckoutState.GetCustomState<PayPalCheckoutState>();
            if (state.PayPalOrderId != null)
            {
                state.SubmitForm = true;

                _checkoutStateAccessor.CheckoutState.CustomProperties["PayPalPayerActionRequired"] = true;
            }
            else
            {
                _checkoutStateAccessor.CheckoutState.RemoveCustomState<PayPalCheckoutState>();
                NotifyWarning(T("Payment.MissingCheckoutState", "PayPalCheckoutState." + nameof(state.PayPalOrderId)));

                return RedirectToAction(nameof(CheckoutController.PaymentMethod), "Checkout");
            }

            return RedirectToAction(nameof(CheckoutController.Confirm), "Checkout");
        }

        public IActionResult RedirectionCancel()
        {
            _checkoutStateAccessor.CheckoutState.RemoveCustomState<PayPalCheckoutState>();
            NotifyWarning(T("Payment.PaymentFailure"));

            return RedirectToAction(nameof(CheckoutController.PaymentMethod), "Checkout");
        }

        #region Google Pay

        /// <summary>
        /// AJAX
        /// Gets the <see cref="GoogleTransactionInfo"> for Google Pay.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetGooglePayTransactionInfo(ProductVariantQuery query, bool? useRewardPoints, string paymentSource, string routeIdent = "")
        {
            // TODO: (mh) What is this huge code for? Seems repetitive. Urgently TBD with MC!
            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            // Only save cart data when we're on shopping cart page.
            if (routeIdent == "ShoppingCart.Cart")
            {
                var warnings = new List<string>();    
                var isCartValid = await _shoppingCartService.SaveCartDataAsync(cart, warnings, query, useRewardPoints, false);

                if (!isCartValid)
                {
                    return Json(new { success = false, message = string.Join(Environment.NewLine, warnings) });
                }
            }

            var transactionInfo = new GoogleTransactionInfo
            {
                CurrencyCode = Services.WorkContext.WorkingCurrency.CurrencyCode,
                //TransactionId = Guid.NewGuid().ToString(),    // We'll skip this for now.
                TotalPriceStatus = "ESTIMATED"                  // INFO: Estimated because it is called from basket. Even on payment page a customer could change the shipping method and thus alter the final price.
            };

            var isVatExempt = await _taxService.IsVatExemptAsync(customer);
            var cartSubTotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart, !isVatExempt);
            var subTotalConverted = _currencyService.ConvertFromPrimaryCurrency(cartSubTotal.SubtotalWithDiscount.Amount, _primaryCurrency);

            foreach (var lineItem in cartSubTotal.LineItems)
            {
                var item = lineItem.Item.Item;
                var amountInclTax = _roundingHelper.Round(lineItem.Subtotal.Tax.Value.PriceGross);
                var amountExclTax = _roundingHelper.Round(lineItem.Subtotal.Tax.Value.PriceNet);
                var convertedUnitPrice = _currencyService.ConvertToWorkingCurrency(isVatExempt ? amountInclTax : amountExclTax);
                
                var displayItem = new DisplayItem
                {
                    Label = item.Product.GetLocalized(x => x.Name),
                    Price = convertedUnitPrice.Amount.ToStringInvariant("F"),
                    Status = GooglePayItemStatus.Final,
                    Type = GooglePayItemType.LineItem
                };

                transactionInfo.DisplayItems = [.. transactionInfo.DisplayItems, displayItem];
            }

            // Display subtotal
            var subtotalDisplayItem = new DisplayItem
            {
                Label = T("Order.SubTotal"),
                Price = subTotalConverted.Amount.ToStringInvariant("F"),
                Status = GooglePayItemStatus.Final,
                Type = GooglePayItemType.Subtotal
            };

            // Display tax
            (Money tax, _) = await _orderCalculationService.GetShoppingCartTaxTotalAsync(cart);
            var cartTax = _currencyService.ConvertFromPrimaryCurrency(tax.Amount, _primaryCurrency);
            var taxDisplayItem = new DisplayItem
            {
                Label = T("Order.Tax"),
                Price = cartTax.Amount.ToStringInvariant("F"),
                Status = GooglePayItemStatus.Final,
                Type = GooglePayItemType.Tax
            };

            // Display shipping
            var shippingTotal = await _orderCalculationService.GetShoppingCartShippingTotalAsync(cart, !isVatExempt);
            var shippingTotalAmount = _currencyService.ConvertFromPrimaryCurrency(
                shippingTotal.ShippingTotal != null ?_roundingHelper.Round(shippingTotal.ShippingTotal.Value.Amount) : 0, _primaryCurrency);

            // Only Price=0 and Status=PENDING when were on cart page.
            var shippingDisplayItem = new DisplayItem
            {
                Label = T("Order.Shipping"),
                Price = shippingTotalAmount.Amount.ToStringInvariant("F"),
                Status = shippingTotalAmount.Amount > 0 ? GooglePayItemStatus.Final : GooglePayItemStatus.Pending,
                Type = GooglePayItemType.LineItem
            };

            // Discounts
            var cartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart);
            var cartTotalConverted = _currencyService.ConvertFromPrimaryCurrency(cartTotal.Total != null ? cartTotal.Total.Value.Amount : 0, _primaryCurrency);
            Money orderTotalDiscountAmount = default;
            if (cartTotal.DiscountAmount > decimal.Zero)
            {
                orderTotalDiscountAmount = _currencyService.ConvertFromPrimaryCurrency(cartTotal.DiscountAmount.Amount, _primaryCurrency);
            }

            Money subTotalDiscountAmount = default;
            if (cartSubTotal.DiscountAmount > decimal.Zero)
            {
                subTotalDiscountAmount = _currencyService.ConvertFromPrimaryCurrency(cartSubTotal.DiscountAmount.Amount, _primaryCurrency);
            }

            decimal discountAmount = _roundingHelper.Round(orderTotalDiscountAmount.Amount + subTotalDiscountAmount.Amount);

            var discountDisplayItem = new DisplayItem
            {
                Label = T("Order.TotalDiscount"),          
                Price = (discountAmount * -1).ToStringInvariant("F"),
                Status = GooglePayItemStatus.Final,
                Type = GooglePayItemType.LineItem
            };

            transactionInfo.DisplayItems = [.. transactionInfo.DisplayItems, subtotalDisplayItem, taxDisplayItem, shippingDisplayItem, discountDisplayItem];
            transactionInfo.TotalPriceLabel = T("ShoppingCart.ItemTotal");
            transactionInfo.TotalPrice = (cartTotal.Total != null ? cartTotalConverted.Amount : subTotalConverted.Amount).ToStringInvariant("F");

            return Json(transactionInfo);
        }

        #endregion

        [HttpPost]
        [Route("paypal/webhookhandler"), WebhookEndpoint]
        public async Task<IActionResult> WebhookHandler()
        {
            string rawRequest = null;

            try
            {
                using (var reader = new StreamReader(Request.Body))
                {
                    rawRequest = await reader.ReadToEndAsync();
                }

                if (rawRequest.HasValue())
                {
                    var webhookEvent = JsonConvert.DeserializeObject<WebhookEvent<WebhookResource>>(rawRequest, PayPalHelper.SerializerSettings);
                    var response = await VerifyWebhookRequest(Request, webhookEvent);
                    var resource = webhookEvent.Resource;

                    var webhookResourceType = webhookEvent.ResourceType?.ToLowerInvariant();

                    // We only handle authorization, capture, refund, checkout-order & order webhooks.
                    // checkout-order & order webhooks are used for APMs.
                    if (webhookResourceType != "authorization"
                        && webhookResourceType != "capture"
                        && webhookResourceType != "refund"
                        && webhookResourceType != "checkout-order"
                        && webhookResourceType != "order")
                    {
                        return Ok();
                    }

                    var customId = resource?.CustomId ?? resource?.PurchaseUnits?[0]?.CustomId;

                    if (!Guid.TryParse(customId, out var orderGuid))
                    {
                        return NotFound();
                    }

                    var order = await _db.Orders.FirstOrDefaultAsync(x => x.OrderGuid == orderGuid);

                    if (order == null)
                    {
                        return NotFound();
                    }

                    if (!order.PaymentMethodSystemName.StartsWith("Payments.PayPal"))
                    {
                        return NotFound();
                    }

                    // Add order note.
                    order.AddOrderNote($"Webhook: {Environment.NewLine}{rawRequest}", false);

                    // Handle transactions.
                    switch (webhookResourceType)
                    {
                        case "authorization":
                            await HandleAuthorizationAsync(order, resource);
                            break;
                        case "capture":
                            await HandleCaptureAsync(order, resource);
                            break;
                        case "refund":
                            await HandleRefundAsync(order, resource);
                            break;
                        case "checkout-order":
                        case "order":
                            await HandleCheckoutOrderAsync(order, resource);
                            break;
                        default:
                            throw new PayPalException("Cannot proccess resource type.");
                    };

                    // Update order.
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, rawRequest);
            }

            return Ok();
        }

        private async Task HandleAuthorizationAsync(Order order, WebhookResource resource)
        {
            var status = resource?.Status.ToLowerInvariant();
            switch (status)
            {
                case "created":
                    if (decimal.TryParse(resource.Amount?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var authorizedAmount)
                        && authorizedAmount == _roundingHelper.Round(order.OrderTotal, 2, _primaryCurrency.MidpointRounding)
                        && order.CanMarkOrderAsAuthorized())
                    {
                        order.AuthorizationTransactionId = resource.Id;
                        order.AuthorizationTransactionResult = status;
                        await _orderProcessingService.MarkAsAuthorizedAsync(order);
                    }
                    break;

                case "denied":
                case "expired":
                case "pending":
                    order.CaptureTransactionResult = status;
                    order.OrderStatus = OrderStatus.Pending;
                    break;

                case "voided":
                    if (order.CanVoidOffline())
                    {
                        order.AuthorizationTransactionId = resource.Id;
                        order.AuthorizationTransactionResult = status;
                        await _orderProcessingService.VoidOfflineAsync(order);
                    }
                    break;
            }
        }

        private async Task HandleCaptureAsync(Order order, WebhookResource resource)
        {
            var status = resource?.Status.ToLowerInvariant();
            switch (status)
            {
                case "completed":
                    if (decimal.TryParse(resource.Amount?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var capturedAmount))
                    {
                        if (order.CanMarkOrderAsPaid() && capturedAmount == _roundingHelper.Round(order.OrderTotal, 2, _primaryCurrency.MidpointRounding))
                        {
                            order.CaptureTransactionId = resource.Id;
                            order.CaptureTransactionResult = status;
                            await _orderProcessingService.MarkOrderAsPaidAsync(order);
                        }
                    }
                    break;

                case "pending":
                    order.CaptureTransactionResult = status;
                    order.OrderStatus = OrderStatus.Pending;
                    break;

                case "declined":
                    var settings = await Services.SettingFactory.LoadSettingsAsync<PayPalSettings>(order.StoreId);
                    order.CaptureTransactionResult = status;
                    if (settings.Intent == PayPalTransactionType.Authorize)
                    {
                        if (order.CanVoidOffline())
                        {
                            await _orderProcessingService.VoidOfflineAsync(order);
                        }
                    }
                    else
                    {   
                        if (settings.CancelOrdersForDeclinedPayments)
                        {
                            order.PaymentStatus = PaymentStatus.Voided;
                            await _orderProcessingService.CancelOrderAsync(order, true);
                        }
                        else
                        {
                            order.PaymentStatus = PaymentStatus.Pending;
                            order.OrderStatus = OrderStatus.Pending;
                        }

                        order.PaidDateUtc = null;
                    }
                    break;

                case "refunded":
                    if (order.CanRefundOffline())
                    {
                        await _orderProcessingService.RefundOfflineAsync(order);
                    }
                    break;
            }
        }

        private async Task HandleCheckoutOrderAsync(Order order, WebhookResource resource)
        {
            var status = resource?.Status.ToLowerInvariant();
            switch (status)
            {
                case "completed":
                    if (decimal.TryParse(resource.Amount?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var capturedAmount))
                    {
                        if (order.CanMarkOrderAsPaid() && capturedAmount == _roundingHelper.Round(order.OrderTotal, 2, _primaryCurrency.MidpointRounding))
                        {
                            order.CaptureTransactionId = resource.Id;
                            order.CaptureTransactionResult = status;
                            await _orderProcessingService.MarkOrderAsPaidAsync(order);
                        }
                    }
                    break;
                case "voided":
                    order.CaptureTransactionResult = status;
                    await _orderProcessingService.VoidAsync(order);
                    break;
            }
        }

        private async Task HandleRefundAsync(Order order, WebhookResource resource)
        {
            var status = resource?.Status.ToLowerInvariant();
            switch (status)
            {
                case "completed":
                    var refundIds = order.GenericAttributes.Get<List<string>>("Payments.PayPalStandard.RefundId") ?? [];
                    if (!refundIds.Contains(resource.Id)
                        && decimal.TryParse(resource.Amount?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var refundedAmount)
                        && order.CanPartiallyRefundOffline(refundedAmount))
                    {
                        await _orderProcessingService.PartiallyRefundOfflineAsync(order, refundedAmount);
                        refundIds.Add(resource.Id);
                        order.GenericAttributes.Set("Payments.PayPalStandard.RefundId", refundIds);
                        await _db.SaveChangesAsync();
                    }
                    break;
            }
        }

        async Task<PayPalResponse> VerifyWebhookRequest(HttpRequest request, WebhookEvent<WebhookResource> webhookEvent)
        {
            var verifyRequest = _client.RequestFactory.WebhookVerifySignature(new VerifyWebhookSignature<WebhookResource>
            {
                AuthAlgo = request.Headers["PAYPAL-AUTH-ALGO"],
                CertUrl = request.Headers["PAYPAL-CERT-URL"],
                TransmissionId = request.Headers["PAYPAL-TRANSMISSION-ID"],
                TransmissionSig = request.Headers["PAYPAL-TRANSMISSION-SIG"],
                TransmissionTime = request.Headers["PAYPAL-TRANSMISSION-TIME"],
                WebhookId = _settings.WebhookId,
                WebhookEvent = webhookEvent
            });

            var response = await _client.ExecuteRequestAsync(verifyRequest);

            // TODO: (mh) (core) OK ain't enough. The response body must contain a "SUCCESS"
            // INFO: won't work for mockups that can be sent with PayPal Webhooks simulator as mockups can't be validated.
            if (response.Status == HttpStatusCode.OK)
            {
                return response;
            }
            else
            {
                throw new PayPalException("Could not verify request.");
            }
        }
    }
}