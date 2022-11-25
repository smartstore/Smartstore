using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Localization;
using Smartstore.StripeElements.Models;
using Smartstore.StripeElements.Settings;
using Smartstore.Web.Components;
using Stripe;

namespace Smartstore.StripeElements.Components
{
    public class StripeElementsViewComponent : SmartViewComponent
    {
        private readonly StripeSettings _settings;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ITaxService _taxService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductService _productService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly ICurrencyService _currencyService;

        public StripeElementsViewComponent(
            StripeSettings settings,
            IShoppingCartService shoppingCartService,
            ITaxService taxService,
            IPriceCalculationService priceCalculationService,
            IProductService productService,
            IOrderCalculationService orderCalculationService,
            ICheckoutStateAccessor checkoutStateAccessor,
            ICurrencyService currencyService)
        {
            _settings = settings;
            _shoppingCartService = shoppingCartService;
            _taxService = taxService;
            _priceCalculationService = priceCalculationService;
            _productService = productService;
            _orderCalculationService = orderCalculationService;
            _checkoutStateAccessor = checkoutStateAccessor;
            _currencyService = currencyService;
        }

        /// <summary>
        /// Renders Stripe Wallet Button Element in OffCanvasCart & cart page and Payment Element on payment selection page.
        /// </summary>
        public async Task<IViewComponentResult> InvokeAsync()
        {
            // If public API key or secret API key haven't been configured yet, don't render anything.
            if (!_settings.PublicApiKey.HasValue() || !_settings.SecrectApiKey.HasValue())
            {
                return Empty();
            }

            var routeIdent = Request.RouteValues.GenerateRouteIdentifier();
            var isPaymentSelectionPage = routeIdent == "Checkout.PaymentMethod";

            var model = new PublicStripeElementsModel
            {
                PublicApiKey = _settings.PublicApiKey,
                IsPaymentSelectionPage = isPaymentSelectionPage
            };

            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var currency = Services.WorkContext.WorkingCurrency;
            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            // Get subtotal
            var cartSubTotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart, true);
            var subTotalConverted = _currencyService.ConvertFromPrimaryCurrency(cartSubTotal.SubtotalWithoutDiscount.Amount, currency);

            if (isPaymentSelectionPage)
            {
                var checkoutState = _checkoutStateAccessor.CheckoutState.GetCustomState<StripeCheckoutState>();
                var paymentIntent = new PaymentIntent();
                if (checkoutState.PaymentIntent == null)
                {
                    var paymentIntentService = new PaymentIntentService();
                    paymentIntent = paymentIntentService.Create(new PaymentIntentCreateOptions
                    {
                        Amount = subTotalConverted.Amount.ToSmallestCurrencyUnit(),
                        Currency = Services.WorkContext.WorkingCurrency.CurrencyCode.ToLower(),
                        CaptureMethod = _settings.CaptureMethod,
                        AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                        {
                            Enabled = true,
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            ["CustomerId"] = customer.Id.ToString()
                        }
                    });

                    checkoutState.PaymentIntent = paymentIntent;
                }
                else
                {
                    paymentIntent = checkoutState.PaymentIntent;
                }
                
                model.ClientSecret = paymentIntent.ClientSecret;

                return View(model);
            }

            var cartProducts = cart.Items.Select(x => x.Item.Product).ToArray();
            var batchContext = _productService.CreateProductBatchContext(cartProducts, null, customer, false);
            var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, customer, currency, batchContext);

            var displayItems = new List<StripePaymentItem>();

            foreach (var item in cart.Items)
            {
                var taxRate = await _taxService.GetTaxRateAsync(item.Item.Product);
                var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(item, calculationOptions);
                var (unitPrice, subtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);

                displayItems.Add(new StripePaymentItem
                {
                    Label = item.Item.Product.GetLocalized(x => x.Name),
                    Amount = subtotal.FinalPrice.Amount.ToSmallestCurrencyUnit(),
                    Pending = false
                });
            }

            // Prepare Stripe payment request object.
            var stripePaymentRequest = new StripePaymentRequest
            {
                Country = Services.WorkContext.WorkingLanguage.UniqueSeoCode.ToUpper(),
                Currency = Services.WorkContext.WorkingCurrency.CurrencyCode.ToLower(),
                Total = new StripePaymentItem
                {
                    Amount = subTotalConverted.Amount.ToSmallestCurrencyUnit(),
                    Label = T("Order.SubTotal").Value,
                    Pending = false
                },
                DisplayItems = displayItems
            };

            model.PaymentRequest = JsonConvert.SerializeObject(stripePaymentRequest); 
        
            return View(model);
        }
    }
}