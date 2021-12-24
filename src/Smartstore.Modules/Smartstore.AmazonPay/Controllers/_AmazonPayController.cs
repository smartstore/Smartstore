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
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
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
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly AmazonPaySettings _settings;
        private readonly OrderSettings _orderSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;

        public AmazonPayController(
            SmartDbContext db,
            IWebStoreClient apiClient,
            IAmazonPayService amazonPayService,
            IShoppingCartService shoppingCartService,
            IShoppingCartValidator shoppingCartValidator,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            ICheckoutStateAccessor checkoutStateAccessor,
            AmazonPaySettings amazonPaySettings,
            OrderSettings orderSettings,
            RewardPointsSettings rewardPointsSettings)
        {
            _db = db;
            _apiClient = apiClient;
            _amazonPayService = amazonPayService;
            _shoppingCartService = shoppingCartService;
            _shoppingCartValidator = shoppingCartValidator;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            _checkoutStateAccessor = checkoutStateAccessor;
            _settings = amazonPaySettings;
            _orderSettings = orderSettings;
            _rewardPointsSettings = rewardPointsSettings;
        }

        /// <summary>
        /// AJAX. Creates the AmazonPay checkout session object after clicking the AmazonPay button.
        /// </summary>
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

                    // Save data entered on cart page.
                    customer.ResetCheckoutData(store.Id);
                    customer.GenericAttributes.CheckoutAttributes = await _checkoutAttributeMaterializer.CreateCheckoutAttributeSelectionAsync(query, cart);

                    if (_rewardPointsSettings.Enabled && useRewardPoints.HasValue)
                    {
                        customer.GenericAttributes.UseRewardPointsDuringCheckout = useRewardPoints.Value;
                    }

                    // INFO: we must save before validating the cart.
                    await _db.SaveChangesAsync();

                    // Validate the shopping cart.
                    if (await _shoppingCartValidator.ValidateCartAsync(cart, warnings, true))
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
        /// The buyer is redirected to this action method after they complete checkout on the AmazonPay hosted page.
        /// </summary>
        [Route("amazonpay/checkoutreview")]
        public async Task<IActionResult> CheckoutReview(string amazonCheckoutSessionId)
        {
            try
            {
                var result = await ProcessCheckoutSession(amazonCheckoutSessionId);
                if (result.Success)
                {
                    var actionName = result.IsShippingRequired && Services.WorkContext.CurrentCustomer.GenericAttributes.SelectedShippingOption == null
                        ? nameof(CheckoutController.ShippingMethod)
                        : nameof(CheckoutController.Confirm);

                    return RedirectToAction(actionName, "Checkout");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                NotifyError(ex);
            }

            return RedirectToRoute("ShoppingCart");
        }

        private async Task<CheckoutReviewResult> ProcessCheckoutSession(string amazonCheckoutSessionId)
        {
            var result = new CheckoutReviewResult();

            if (amazonCheckoutSessionId.IsEmpty())
            {
                NotifyWarning(T("Plugins.Payments.AmazonPay.MissingCheckoutSessionId"));
                return result;
            }

            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            result.IsShippingRequired = cart.IsShippingRequired();

            if (!cart.HasItems || (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
            {
                return result;
            }

            await _db.LoadCollectionAsync(customer, x => x.Addresses);

            // Create addresses from AmazonPay checkout session.
            var session = _apiClient.GetCheckoutSession(amazonCheckoutSessionId);           

            var billTo = await _amazonPayService.CreateAddressAsync(session, customer, true);
            if (!billTo.Success)
            {
                // INFO: this is not nice. We have to kick the buyer out and redirect him back to the shopping cart because
                // he cannot change the address here. We cannot store invalid addresses and assign them to a customer.
                // Also, the shipping method has not been selected yet.
                NotifyWarning(T("Plugins.Payments.AmazonPay.BillingToCountryNotAllowed"));
                return result;
            }

            CheckoutAdressResult shipTo = null;

            if (result.IsShippingRequired)
            {
                shipTo = await _amazonPayService.CreateAddressAsync(session, customer, false);
                if (!shipTo.Success)
                {
                    NotifyWarning(T("Plugins.Payments.AmazonPay.ShippingToCountryNotAllowed"));
                    return result;
                }
            }


            // Update customer.
            var billingAddress = customer.FindAddress(billTo.Address);
            if (billingAddress != null)
            {
                customer.BillingAddress = billingAddress;
            }
            else
            {
                customer.Addresses.Add(billTo.Address);
                customer.BillingAddress = billTo.Address;
            }

            if (shipTo == null)
            {
                customer.ShippingAddress = null;
            }
            else
            {
                var shippingAddress = customer.FindAddress(shipTo.Address);
                if (shippingAddress != null)
                {
                    customer.ShippingAddress = shippingAddress;
                }
                else
                {
                    customer.Addresses.Add(shipTo.Address);
                    customer.ShippingAddress = shipTo.Address;
                }
            }

            if (_settings.CanSaveEmailAndPhone(customer.Email))
            {
                customer.Email = session.Buyer.Email;
            }

            if (_settings.CanSaveEmailAndPhone(customer.GenericAttributes.Phone))
            {
                customer.GenericAttributes.Phone = billTo.Address.PhoneNumber.NullEmpty() ?? session.Buyer.PhoneNumber;
            }

            await _db.SaveChangesAsync();
            result.Success = true;

            if (session.PaymentPreferences != null)
            {
                _checkoutStateAccessor.CheckoutState.PaymentSummary = string.Join(", ", session.PaymentPreferences.Select(x => x.PaymentDescriptor));
            }

            return result;
        }

        /// <summary>
        /// AJAX.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(string formData)
        {
            var result = new CheckoutConfirmResult();

            try
            {
                result = await ProcessConfirmation(formData);
            }
            catch (Exception ex)
            {
                result.Messages.Add(ex.Message);
                Logger.Error(ex);
            }

            return Json(new
            {
                success = result.Success,
                redirectUrl = result.RedirectUrl,
                messages = result.Messages
            });
        }

        private async Task<CheckoutConfirmResult> ProcessConfirmation(string formData)
        {
            var result = new CheckoutConfirmResult();
            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            // Validate countries.
            await _db.LoadReferenceAsync(customer, x => x.BillingAddress, false, q => q.Include(x => x.Country));

            if (!customer.BillingAddress.Country.AllowsBilling)
            {
                result.Messages.Add(T("Plugins.Payments.AmazonPay.BillingToCountryNotAllowed"));
                return result;
            }

            if (cart.IsShippingRequired())
            {
                await _db.LoadReferenceAsync(customer, x => x.ShippingAddress, false, q => q.Include(x => x.Country));

                if (!customer.ShippingAddress.Country.AllowsShipping)
                {
                    result.Messages.Add(T("Plugins.Payments.AmazonPay.ShippingToCountryNotAllowed"));
                    return result;
                }
            }

            //......

            result.Success = true;
            return result;
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
