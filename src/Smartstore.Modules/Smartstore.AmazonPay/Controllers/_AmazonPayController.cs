using System.Linq;
using Amazon.Pay.API.WebStore.Buyer;
using Amazon.Pay.API.WebStore.CheckoutSession;
using Amazon.Pay.API.WebStore.Interfaces;
using Amazon.Pay.API.WebStore.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;
using Smartstore.Web.Controllers;

namespace Smartstore.AmazonPay.Controllers
{
    public class AmazonPayController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IWebStoreClient _apiClient;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly AmazonPaySettings _settings;

        public AmazonPayController(
            SmartDbContext db,
            IWebStoreClient apiClient,
            IShoppingCartService shoppingCartService,
            AmazonPaySettings amazonPaySettings)
        {
            _db = db;
            _apiClient = apiClient;
            _shoppingCartService = shoppingCartService;
            _settings = amazonPaySettings;
        }

        // AJAX.
        [HttpPost]
        public async Task<IActionResult> CreateCheckoutSession(string buttonType)
        {
            Guard.NotEmpty(buttonType, nameof(buttonType));

            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var currentScheme = Services.WebHelper.IsCurrentConnectionSecured() ? "https" : "http";

            string signature;
            string payload;

            if (buttonType == "PayAndShip" || buttonType == "PayOnly")
            {
                var checkoutReviewUrl = Url.Action(nameof(CheckoutReview), "AmazonPay", null, currentScheme);
                
                // TODO later: config for specialRestrictions 'RestrictPOBoxes', 'RestrictPackstations'.
                var request = new CreateCheckoutSessionRequest(checkoutReviewUrl, _settings.ClientId)
                {
                    ChargePermissionType = ChargePermissionType.OneTime
                };

                var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
                if (cart.HasItems && cart.IsShippingRequired())
                {
                    var allowedCountryCodes = await _db.Countries
                        .ApplyStandardFilter(false, store.Id)
                        .Select(x => x.TwoLetterIsoCode)
                        .Distinct()
                        .ToListAsync();

                    if (allowedCountryCodes.Any())
                    {
                        request.DeliverySpecifications.AddressRestrictions.Type = RestrictionType.Allowed;
                        allowedCountryCodes.Each(countryCode => request.DeliverySpecifications.AddressRestrictions.AddCountryRestriction(countryCode));
                    }
                }

                signature = _apiClient.GenerateButtonSignature(request);
                payload = request.ToJson();
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

                signature = _apiClient.GenerateButtonSignature(request);
                payload = request.ToJson();
            }
            else
            {
                throw new ArgumentException($"Unknown or not supported button type '{buttonType}'.");
            }

            return Json(new { signature, payload });
        }

        /// <summary>
        /// The buyer is redirected to this action method after they complete checkout on the Amazon Pay hosted page.
        /// </summary>
        [Route("amazonpay/checkoutreview")]
        public Task<IActionResult> CheckoutReview()
        {
            throw new NotImplementedException();
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
