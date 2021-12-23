using System.Linq;
using Amazon.Pay.API.WebStore.Buyer;
using Amazon.Pay.API.WebStore.CheckoutSession;
using Amazon.Pay.API.WebStore.Interfaces;
using Amazon.Pay.API.WebStore.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.AmazonPay.Services;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.Web.Controllers;

namespace Smartstore.AmazonPay.Controllers
{
    public class AmazonPayController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IWebStoreClient _apiClient;
        private readonly IAmazonPayService _amazonPayService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly AmazonPaySettings _settings;

        public AmazonPayController(
            SmartDbContext db,
            IWebStoreClient apiClient,
            IAmazonPayService amazonPayService,
            IShoppingCartService shoppingCartService,
            IShoppingCartValidator shoppingCartValidator,
            ICheckoutStateAccessor checkoutStateAccessor,
            AmazonPaySettings amazonPaySettings)
        {
            _db = db;
            _apiClient = apiClient;
            _amazonPayService = amazonPayService;
            _shoppingCartService = shoppingCartService;
            _shoppingCartValidator = shoppingCartValidator;
            _checkoutStateAccessor = checkoutStateAccessor;
            _settings = amazonPaySettings;
        }

        // AJAX.
        [HttpPost]
        public async Task<IActionResult> CreateCheckoutSession(ProductVariantQuery query, string buttonType, bool? useRewardPoints)
        {
            Guard.NotEmpty(buttonType, nameof(buttonType));

            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var currentScheme = Services.WebHelper.IsCurrentConnectionSecured() ? "https" : "http";

            var signature = string.Empty;
            var payload = string.Empty;
            var message = string.Empty;
            var messageType = "error";
            var success = false;

            try
            {
                if (buttonType == "PayAndShip" || buttonType == "PayOnly")
                {
                    var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
                    var warnings = new List<string>();

                    // Save checkout attributes, whether to use reward points and validate the shopping cart.
                    if (await _shoppingCartValidator.ValidateCartAsync(cart, warnings, true, query, useRewardPoints ?? false))
                    {
                        // TODO later: config for specialRestrictions 'RestrictPOBoxes', 'RestrictPackstations'.
                        var checkoutReviewUrl = Url.Action(nameof(CheckoutReview), "AmazonPay", null, currentScheme);
                        var request = new CreateCheckoutSessionRequest(checkoutReviewUrl, _settings.ClientId)
                        {
                            PlatformId = AmazonPayService.PlatformId
                        };

                        if (cart.HasItems && cart.IsShippingRequired())
                        {
                            var allowedCountryCodes = await _db.Countries
                                .ApplyStandardFilter(false, store.Id)
                                .Where(x => x.AllowsBilling || x.AllowsShipping)
                                .Select(x => x.TwoLetterIsoCode)
                                .Distinct()
                                .ToListAsync();

                            if (allowedCountryCodes.Any())
                            {
                                request.DeliverySpecifications.AddressRestrictions.Type = RestrictionType.Allowed;
                                allowedCountryCodes.Each(countryCode => request.DeliverySpecifications.AddressRestrictions.AddCountryRestriction(countryCode));
                            }
                        }

                        payload = request.ToJsonNoType();
                        signature = _apiClient.GenerateButtonSignature(payload);
                        success = true;
                    }
                    else
                    {
                        messageType = "warning";
                        message = string.Join(Environment.NewLine, warnings);
                        success = false;
                    }
                }
                else if (buttonType == "SignIn")
                {
                    var signInReturnUrl = Url.Action(nameof(SignIn), "AmazonPay", null, currentScheme);

                    var request = new SignInRequest(signInReturnUrl, _settings.ClientId)
                    {
                        SignInScopes = new[]
                        {
                            SignInScope.Name,
                            SignInScope.Email,
                            //SignInScope.PostalCode, 
                            SignInScope.ShippingAddress,
                            SignInScope.BillingAddress,
                            SignInScope.PhoneNumber
                        }
                    };

                    payload = request.ToJsonNoType();
                    signature = _apiClient.GenerateButtonSignature(payload);
                    success = true;
                }
                else
                {
                    throw new ArgumentException($"Unknown or not supported button type '{buttonType}'.");
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                success = false;
            }

            return Json(new { success, signature, payload, message, messageType });
        }

        /// <summary>
        /// The buyer is redirected to this action method after they complete checkout on the Amazon Pay hosted page.
        /// </summary>
        [Route("amazonpay/checkoutreview")]
        public async Task<IActionResult> CheckoutReview(string amazonCheckoutSessionId)
        {
            if (amazonCheckoutSessionId.IsEmpty())
            {
                NotifyWarning(T("Plugins.Payments.AmazonPay.MissingCheckoutSessionId"));
                return RedirectToRoute("ShoppingCart");
            }

            try
            {
                await ProcessCheckoutReview(amazonCheckoutSessionId);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                NotifyError(ex);
            }

            return RedirectToAction(nameof(CheckoutController.Confirm), "Checkout");
        }

        private async Task<bool> ProcessCheckoutReview(string amazonCheckoutSessionId)
        {
            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
            var session = _apiClient.GetCheckoutSession(amazonCheckoutSessionId);
            var shippingRequired = cart.IsShippingRequired();
            
            await _db.LoadCollectionAsync(customer, x => x.Addresses);

            var billTo = await _amazonPayService.CreateAddressAsync(session, customer, true);
            if (billTo == null)
            {
                NotifyWarning(T("Plugins.Payments.AmazonPay.BillingToCountryNotAllowed"));
                return false;
            }

            var shipTo = shippingRequired
                ? await _amazonPayService.CreateAddressAsync(session, customer, false)
                : null;

            if (shippingRequired && shipTo == null)
            {
                NotifyWarning(T("Plugins.Payments.AmazonPay.ShippingToCountryNotAllowed"));
                return false;
            }

            // Update customer.
            var billingAddress = customer.FindAddress(billTo);
            if (billingAddress != null)
            {
                customer.BillingAddress = billingAddress;
            }
            else
            {
                customer.Addresses.Add(billTo);
                customer.BillingAddress = billTo;
            }

            if (shipTo == null)
            {
                customer.ShippingAddress = null;
            }
            else
            {
                var shippingAddress = customer.FindAddress(shipTo);
                if (shippingAddress != null)
                {
                    customer.ShippingAddress = shippingAddress;
                }
                else
                {
                    customer.Addresses.Add(shipTo);
                    customer.ShippingAddress = shipTo;
                }
            }

            if (_settings.CanSaveEmailAndPhone(customer.Email))
            {
                customer.Email = session.Buyer.Email;
            }

            if (_settings.CanSaveEmailAndPhone(customer.GenericAttributes.Phone))
            {
                customer.GenericAttributes.Phone = billTo.PhoneNumber.NullEmpty() ?? session.Buyer.PhoneNumber;
            }

            await _db.SaveChangesAsync();

            if (session.PaymentPreferences != null)
            {
                _checkoutStateAccessor.CheckoutState.PaymentSummary = string.Join(", ", session.PaymentPreferences.Select(x => x.PaymentDescriptor));
            }

            return true;
        }

        /// <summary>
        /// The buyer is redirected to this action method after they click the sign-in button.
        /// </summary>
        [Route("amazonpay/signin")]
        public Task<IActionResult> SignIn()
        {
            throw new NotImplementedException();
        }
    }
}
