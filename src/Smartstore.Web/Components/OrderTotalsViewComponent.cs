using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Cart.Events;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Localization;
using Smartstore.Web.Models.Cart;

namespace Smartstore.Web.Components
{
    /// <summary>
    /// Component for rendering order totals.
    /// </summary>
    public class OrderTotalsViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly ITaxService _taxService;
        private readonly ITaxCalculator _taxCalculator;
        private readonly ICurrencyService _currencyService;
        private readonly IGiftCardService _giftCardService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IShippingService _shippingService;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly MeasureSettings _measureSettings;
        private readonly TaxSettings _taxSettings;

        public OrderTotalsViewComponent(
            SmartDbContext db,
            ITaxService taxService,
            ITaxCalculator taxCalculator,
            ICurrencyService currencyService,
            IGiftCardService giftCardService,
            IShoppingCartService shoppingCartService,
            ILocalizationService localizationService,
            IOrderCalculationService orderCalculationService,
            IShippingService shippingService,
            ShoppingCartSettings shoppingCartSettings,
            MeasureSettings measureSettings,
            TaxSettings taxSettings)
        {
            _db = db;
            _taxService = taxService;
            _taxCalculator = taxCalculator;
            _currencyService = currencyService;
            _giftCardService = giftCardService;
            _shoppingCartService = shoppingCartService;
            _localizationService = localizationService;
            _orderCalculationService = orderCalculationService;
            _shippingService = shippingService;
            _shoppingCartSettings = shoppingCartSettings;
            _measureSettings = measureSettings;
            _taxSettings = taxSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync(bool isEditable = false)
        {
            var orderTotalsEvent = new RenderingOrderTotalsEvent();
            await Services.EventPublisher.PublishAsync(orderTotalsEvent);

            var currency = Services.WorkContext.WorkingCurrency;
            var customer = orderTotalsEvent.Customer ?? Services.WorkContext.CurrentCustomer;
            var storeId = orderTotalsEvent.StoreId ?? Services.StoreContext.CurrentStore.Id;

            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, storeId);
            var model = new OrderTotalsModel
            {
                IsEditable = isEditable
            };

            if (!cart.Items.Any())
            {
                return View(model);
            }

            model.Weight = await _shippingService.GetCartTotalWeightAsync(cart);

            var measureWeight = await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId, false);
            if (measureWeight != null)
            {
                model.WeightMeasureUnitName = measureWeight.GetLocalized(x => x.Name);
            }

            // SubTotal
            var cartSubTotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart);
            var subTotalConverted = _currencyService.ConvertFromPrimaryCurrency(cartSubTotal.SubtotalWithoutDiscount.Amount, currency);

            model.SubTotal = subTotalConverted;

            if (cartSubTotal.DiscountAmount > decimal.Zero)
            {
                var subTotalDiscountAmountConverted = _currencyService.ConvertFromPrimaryCurrency(cartSubTotal.DiscountAmount.Amount, currency);

                model.SubTotalDiscount = subTotalDiscountAmountConverted * -1;
                model.AllowRemovingSubTotalDiscount = cartSubTotal.AppliedDiscount != null
                    && cartSubTotal.AppliedDiscount.RequiresCouponCode
                    && cartSubTotal.AppliedDiscount.CouponCode.HasValue()
                    && model.IsEditable;
            }

            // Shipping info
            model.RequiresShipping = cart.IsShippingRequired();
            if (model.RequiresShipping)
            {
                var shippingTotal = await _orderCalculationService.GetShoppingCartShippingTotalAsync(cart);
                if (shippingTotal.ShippingTotal.HasValue)
                {
                    var shippingTotalConverted = _currencyService.ConvertFromPrimaryCurrency(shippingTotal.ShippingTotal.Value.Amount, currency);
                    model.Shipping = shippingTotalConverted.ToString();

                    // Selected shipping method
                    var shippingOption = customer.GenericAttributes.SelectedShippingOption;
                    if (shippingOption != null)
                        model.SelectedShippingMethod = shippingOption.Name;
                }
            }

            // Payment method fee
            var paymentFee = await _orderCalculationService.GetShoppingCartPaymentFeeAsync(cart, customer.GenericAttributes.SelectedPaymentMethod);
            var paymentFeeTax = await _taxCalculator.CalculatePaymentFeeTaxAsync(paymentFee.Amount, customer: customer);
            if (paymentFeeTax.Price != 0m)
            {
                var convertedPaymentFeeTax = _currencyService.ConvertFromPrimaryCurrency(paymentFeeTax.Price, currency);
                model.PaymentMethodAdditionalFee = convertedPaymentFeeTax;
            }

            // Tax
            var displayTax = true;
            var displayTaxRates = true;
            if (_taxSettings.HideTaxInOrderSummary && Services.WorkContext.TaxDisplayType == TaxDisplayType.IncludingTax)
            {
                displayTax = false;
                displayTaxRates = false;
            }
            else
            {
                (Money price, TaxRatesDictionary taxRates) = await _orderCalculationService.GetShoppingCartTaxTotalAsync(cart);
                var cartTax = _currencyService.ConvertFromPrimaryCurrency(price.Amount, currency);

                if (price == decimal.Zero && _taxSettings.HideZeroTax)
                {
                    displayTax = false;
                    displayTaxRates = false;
                }
                else
                {
                    displayTaxRates = _taxSettings.DisplayTaxRates && taxRates.Count > 0;
                    displayTax = !displayTaxRates;
                    model.Tax = cartTax.ToString(true);

                    foreach (var taxRate in taxRates)
                    {
                        var rate = _taxService.FormatTaxRate(taxRate.Key);
                        var labelKey = "ShoppingCart.Totals.TaxRateLine" + (Services.WorkContext.TaxDisplayType == TaxDisplayType.IncludingTax ? "Incl" : "Excl");
                        model.TaxRates.Add(new OrderTotalsModel.TaxRate
                        {
                            Rate = rate,
                            Value = _currencyService.ConvertFromPrimaryCurrency(taxRate.Value, currency),
                            Label = _localizationService.GetResource(labelKey).FormatCurrent(rate)
                        });
                    }
                }
            }

            model.DisplayTaxRates = displayTaxRates;
            model.DisplayTax = displayTax;

            model.DisplayWeight = _shoppingCartSettings.ShowWeight;
            model.ShowConfirmOrderLegalHint = _shoppingCartSettings.ShowConfirmOrderLegalHint;

            // Cart total
            var cartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart);

            if (cartTotal.ConvertedAmount.Total.HasValue)
            {
                model.OrderTotal = cartTotal.ConvertedAmount.Total.Value;
                if (cartTotal.ConvertedAmount.ToNearestRounding != decimal.Zero)
                {
                    model.OrderTotalRounding = cartTotal.ConvertedAmount.ToNearestRounding;
                }
            }

            // Discount
            if (cartTotal.DiscountAmount > decimal.Zero)
            {
                var orderTotalDiscountAmount = _currencyService.ConvertFromPrimaryCurrency(cartTotal.DiscountAmount.Amount, currency);
                model.OrderTotalDiscount = orderTotalDiscountAmount * -1;
                model.AllowRemovingOrderTotalDiscount = cartTotal.AppliedDiscount != null
                    && cartTotal.AppliedDiscount.RequiresCouponCode
                    && cartTotal.AppliedDiscount.CouponCode.HasValue()
                    && model.IsEditable;
            }

            // Gift cards
            if (!cartTotal.AppliedGiftCards.IsNullOrEmpty())
            {
                foreach (var appliedGiftCard in cartTotal.AppliedGiftCards)
                {
                    if (appliedGiftCard?.GiftCard == null)
                        continue;

                    var gcModel = new OrderTotalsModel.GiftCard
                    {
                        Id = appliedGiftCard.GiftCard.Id,
                        CouponCode = appliedGiftCard.GiftCard.GiftCardCouponCode,
                    };

                    var amountCanBeUsed = _currencyService.ConvertFromPrimaryCurrency(appliedGiftCard.UsableAmount.Amount, currency);
                    gcModel.Amount = amountCanBeUsed * -1;

                    var remainingAmountBase = await _giftCardService.GetRemainingAmountAsync(appliedGiftCard.GiftCard) - appliedGiftCard.UsableAmount;
                    var remainingAmount = _currencyService.ConvertFromPrimaryCurrency(remainingAmountBase.Amount, currency);
                    gcModel.Remaining = remainingAmount;

                    model.GiftCards.Add(gcModel);
                }
            }

            // Reward points
            if (cartTotal.RedeemedRewardPointsAmount > decimal.Zero)
            {
                var redeemedRewardPointsAmountInCustomerCurrency = _currencyService.ConvertFromPrimaryCurrency(cartTotal.RedeemedRewardPointsAmount.Amount, currency);
                model.RedeemedRewardPoints = cartTotal.RedeemedRewardPoints;
                model.RedeemedRewardPointsAmount = (redeemedRewardPointsAmountInCustomerCurrency * -1).ToString(true);
            }

            // Credit balance.
            if (cartTotal.CreditBalance > decimal.Zero)
            {
                var convertedCreditBalance = _currencyService.ConvertFromPrimaryCurrency(cartTotal.CreditBalance.Amount, currency);
                model.CreditBalance = (convertedCreditBalance * -1).ToString(true);
            }

            return View(model);
        }
    }
}
