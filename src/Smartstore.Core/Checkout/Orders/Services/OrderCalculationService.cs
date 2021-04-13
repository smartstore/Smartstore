using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class OrderCalculationService : IOrderCalculationService
    {
        private const string CART_TAXING_INFO_KEY = "CartTaxingInfos";

        private readonly SmartDbContext _db;
        private readonly IPriceCalculationService2 _priceCalculationService;
        private readonly IDiscountService _discountService;
        private readonly IShippingService _shippingService;
        private readonly IGiftCardService _giftCardService;
        private readonly ICurrencyService _currencyService;
        private readonly IProviderManager _providerManager;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ITaxService _taxService;
        private readonly ITaxCalculator _taxCalculator;
        private readonly TaxSettings _taxSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly Currency _primaryCurrency;
        private readonly Currency _workingCurrency;

        public OrderCalculationService(
            SmartDbContext db,
            IPriceCalculationService2 priceCalculationService,
            IDiscountService discountService,
            IShippingService shippingService,
            IGiftCardService giftCardService,
            ICurrencyService currencyService,
            IProviderManager providerManager,
            IProductAttributeMaterializer productAttributeMaterializer,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            IWorkContext workContext,
            IStoreContext storeContext,
            ITaxService taxService,
            ITaxCalculator taxCalculator,
            TaxSettings taxSettings,
            RewardPointsSettings rewardPointsSettings,
            CatalogSettings catalogSettings,
            ShippingSettings shippingSettings)
        {
            _db = db;
            _priceCalculationService = priceCalculationService;
            _discountService = discountService;
            _shippingService = shippingService;
            _giftCardService = giftCardService;
            _currencyService = currencyService;
            _providerManager = providerManager;
            _productAttributeMaterializer = productAttributeMaterializer;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            _workContext = workContext;
            _storeContext = storeContext;
            _taxService = taxService;
            _taxCalculator = taxCalculator;
            _taxSettings = taxSettings;
            _rewardPointsSettings = rewardPointsSettings;
            _catalogSettings = catalogSettings;
            _shippingSettings = shippingSettings;

            _primaryCurrency = currencyService.PrimaryCurrency;
            _workingCurrency = workContext.WorkingCurrency;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual async Task<ShoppingCartTotal> GetShoppingCartTotalAsync(
            IList<OrganizedShoppingCartItem> cart,
            bool includeRewardPoints = true,
            bool includePaymentFee = true,
            bool includeCreditBalance = true)
        {
            Guard.NotNull(cart, nameof(cart));

            var store = _storeContext.CurrentStore;
            var customer = cart.GetCustomer();

            var paymentMethodSystemName = customer != null
                ? customer.GenericAttributes.SelectedPaymentMethod
                : string.Empty;

            var (_, subTotalWithDiscount, _, _, _) = await GetCartSubTotalAsync(cart, false);

            // Subtotal with discount.
            var subTotalBase = subTotalWithDiscount;

            // Shipping without tax.
            var (shoppingCartShipping, _, _) = await GetCartShippingTotalAsync(cart, false);

            // Payment method additional fee without tax.
            var paymentFeeWithoutTax = decimal.Zero;
            if (includePaymentFee && paymentMethodSystemName.HasValue())
            {
                var paymentFee = await GetShoppingCartPaymentFeeAsync(cart, paymentMethodSystemName);
                if (paymentFee != decimal.Zero)
                {
                    var (paymentFeeExclTax, _) = await _taxService.GetPaymentMethodFeeAsync(paymentFee, false, customer: customer);
                    paymentFeeWithoutTax = paymentFeeExclTax.Amount;
                }
            }

            // Tax.
            var (shoppingCartTax, _) = await GetCartTaxTotalAsync(cart, includePaymentFee);

            // Order total.
            var resultTemp = subTotalBase;

            if (shoppingCartShipping.HasValue)
            {
                resultTemp += shoppingCartShipping.Value;
            }

            resultTemp = _workingCurrency.RoundIfEnabledFor(resultTemp + paymentFeeWithoutTax + shoppingCartTax);

            // Order total discount.
            var (discountAmount, appliedDiscount) = await GetDiscountAmountAsync(resultTemp, DiscountType.AssignedToOrderTotal, customer);

            // Subtotal with discount.
            if (resultTemp < discountAmount)
            {
                discountAmount = resultTemp;
            }

            // Reduce subtotal.
            resultTemp = _workingCurrency.RoundIfEnabledFor(Math.Max(resultTemp - discountAmount, decimal.Zero));

            // Applied gift cards.
            var appliedGiftCards = new List<AppliedGiftCard>();
            if (!cart.IncludesMatchingItems(x => x.IsRecurring))
            {
                // TODO: (ms) (core) Gift card usage in OrderCalculationService needs to be tested extensively as the gift card code has been fundamentally changed.
                var giftCards = await _giftCardService.GetValidGiftCardsAsync(store.Id, customer);
                foreach (var gc in giftCards)
                {
                    if (resultTemp > decimal.Zero)
                    {
                        var usableAmount = resultTemp > gc.UsableAmount.Amount ? gc.UsableAmount.Amount : resultTemp;

                        // Reduce subtotal.
                        resultTemp -= usableAmount;

                        appliedGiftCards.Add(new()
                        {
                            GiftCard = gc.GiftCard,
                            UsableAmount = new(usableAmount, _primaryCurrency)
                        });
                    }
                }
            }

            // Reward points.
            var redeemedRewardPoints = 0;
            var redeemedRewardPointsAmount = decimal.Zero;

            if (_rewardPointsSettings.Enabled && 
                includeRewardPoints && 
                resultTemp > decimal.Zero &&
                customer != null && 
                customer.GenericAttributes.UseRewardPointsDuringCheckout)
            {
                var rewardPointsBalance = customer.GetRewardPointsBalance();
                var rewardPointsBalanceAmount = ConvertRewardPointsToAmountCore(rewardPointsBalance);

                if (resultTemp > rewardPointsBalanceAmount)
                {
                    redeemedRewardPointsAmount = rewardPointsBalanceAmount;
                    redeemedRewardPoints = rewardPointsBalance;
                }
                else
                {
                    redeemedRewardPointsAmount = resultTemp;
                    redeemedRewardPoints = ConvertAmountToRewardPoints(redeemedRewardPointsAmount);
                }
            }

            resultTemp = _workingCurrency.RoundIfEnabledFor(Math.Max(resultTemp, decimal.Zero));

            // Return null if we have errors:
            decimal? orderTotal = shoppingCartShipping.HasValue ? resultTemp : null;
            var orderTotalConverted = orderTotal;
            var appliedCreditBalance = decimal.Zero;
            var toNearestRounding = decimal.Zero;
            var toNearestRoundingConverted = decimal.Zero;

            if (orderTotal.HasValue)
            {
                orderTotal = orderTotal.Value - redeemedRewardPointsAmount;

                // Credit balance.
                if (includeCreditBalance && customer != null && orderTotal > decimal.Zero)
                {
                    var creditBalance = customer.GenericAttributes.UseCreditBalanceDuringCheckout;
                    if (creditBalance > decimal.Zero)
                    {
                        if (creditBalance > orderTotal)
                        {
                            // Normalize used amount.
                            appliedCreditBalance = orderTotal.Value;

                            customer.GenericAttributes.UseCreditBalanceDuringCheckout = orderTotal.Value;
                            await _db.SaveChangesAsync();
                        }
                        else
                        {
                            appliedCreditBalance = creditBalance;
                        }
                    }
                }

                orderTotal = _workingCurrency.RoundIfEnabledFor(orderTotal.Value - appliedCreditBalance);
                orderTotalConverted = _currencyService.ConvertToWorkingCurrency(orderTotal.Value).Amount;

                // Round order total to nearest (cash rounding).
                if (_workingCurrency.RoundOrderTotalEnabled && paymentMethodSystemName.HasValue())
                {
                    var paymentMethod = await _db.PaymentMethods.AsNoTracking().FirstOrDefaultAsync(x => x.PaymentMethodSystemName == paymentMethodSystemName);
                    if (paymentMethod?.RoundOrderTotalEnabled ?? false)
                    {
                        orderTotal = _workingCurrency.RoundToNearest(orderTotal.Value, out toNearestRounding);
                        orderTotalConverted = _workingCurrency.RoundToNearest(orderTotalConverted.Value, out toNearestRoundingConverted);
                    }
                }
            }

            var result = new ShoppingCartTotal
            {
                Total = orderTotal.HasValue ? new(orderTotal.Value, _primaryCurrency) : null,
                ToNearestRounding = new(toNearestRounding, _primaryCurrency),
                DiscountAmount = new(discountAmount, _primaryCurrency),
                AppliedDiscount = appliedDiscount,
                RedeemedRewardPoints = redeemedRewardPoints,
                RedeemedRewardPointsAmount = new(redeemedRewardPointsAmount, _primaryCurrency),
                CreditBalance = new(appliedCreditBalance, _primaryCurrency),
                AppliedGiftCards = appliedGiftCards,
                ConvertedAmount = new ShoppingCartTotal.ConvertedAmounts
                {
                    Total = orderTotalConverted.HasValue ? new(orderTotalConverted.Value, _workingCurrency) : null,
                    ToNearestRounding = new(toNearestRoundingConverted, _workingCurrency)
                }
            };

            return result;
        }

        public virtual async Task<ShoppingCartSubTotal> GetShoppingCartSubTotalAsync(IList<OrganizedShoppingCartItem> cart, bool? includeTax = null)
        {
            includeTax ??= _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;

            var (subTotalWithoutDiscount, subTotalWithDiscount, discountAmount, appliedDiscount, taxRates) = await GetCartSubTotalAsync(cart, includeTax.Value);

            var result = new ShoppingCartSubTotal
            {
                SubTotalWithoutDiscount = new(subTotalWithoutDiscount, _primaryCurrency),
                SubTotalWithDiscount = new(subTotalWithDiscount, _primaryCurrency),
                DiscountAmount = new(discountAmount, _primaryCurrency),
                AppliedDiscount = appliedDiscount,
                TaxRates = taxRates
            };

            return result;
        }

        public virtual async Task<ShoppingCartShippingTotal> GetShoppingCartShippingTotalAsync(IList<OrganizedShoppingCartItem> cart, bool? includeTax = null)
        {
            includeTax ??= _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;

            var (shippingTotal, appliedDiscount, taxRate) = await GetCartShippingTotalAsync(cart, includeTax.Value);

            return new ShoppingCartShippingTotal
            {
                ShippingTotal = shippingTotal.HasValue ? new(shippingTotal.Value, _primaryCurrency) : null,
                AppliedDiscount = appliedDiscount,
                TaxRate = taxRate
            };
        }

        public virtual async Task<(Money Price, TaxRatesDictionary TaxRates)> GetShoppingCartTaxTotalAsync(IList<OrganizedShoppingCartItem> cart, bool includePaymentFee = true)
        {
            var (amount, taxRates) = await GetCartTaxTotalAsync(cart, includePaymentFee);

            return (new(amount, _primaryCurrency), taxRates);
        }

        public virtual async Task<bool> IsFreeShippingAsync(IList<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(cart, nameof(cart));

            var customer = cart.GetCustomer();
            if (customer != null)
            {
                // Check whether customer is in a customer role with free shipping applied.
                var customerRoles = customer.CustomerRoleMappings
                    .Select(x => x.CustomerRole)
                    .Where(x => x.Active);

                if (customerRoles.Any(x => x.FreeShipping))
                {
                    return true;
                }
            }

            if (!cart.IsShippingRequired())
            {
                return true;
            }

            // Check whether there is at least one item with chargeable shipping.
            if (!cart.Any(x => x.Item.IsShippingEnabled && !x.Item.IsFreeShipping))
            {
                return true;
            }

            // Check if the subtotal is large enough for free shipping.
            if (_shippingSettings.FreeShippingOverXEnabled)
            {
                var (_, subTotalWithDiscount, _, _, _) = await GetCartSubTotalAsync(cart, _shippingSettings.FreeShippingOverXIncludingTax);

                if (subTotalWithDiscount > _shippingSettings.FreeShippingOverXValue)
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<Money> GetShoppingCartShippingChargeAsync(IList<OrganizedShoppingCartItem> cart)
            => new(await GetShippingChargeAsync(cart), _primaryCurrency);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<Money> GetShoppingCartPaymentFeeAsync(IList<OrganizedShoppingCartItem> cart, string paymentMethodSystemName)
            => new(await GetCartPaymentFeeAsync(cart, paymentMethodSystemName), _primaryCurrency);

        public virtual async Task<(Money Amount, Discount AppliedDiscount)> AdjustShippingRateAsync(
            IList<OrganizedShoppingCartItem> cart,
            Money shippingRate,
            ShippingOption shippingOption,
            IList<ShippingMethod> shippingMethods)
        {
            var (amount, appliedDiscount) = await AdjustShippingRateAsync(cart, shippingRate.Amount, shippingOption, shippingMethods);

            return (new(amount, _primaryCurrency), appliedDiscount);
        }

        public virtual async Task<(Money Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(Money amount, DiscountType discountType, Customer customer)
        {
            var (discountAmount, appliedDiscount) = await GetDiscountAmountAsync(amount.Amount, discountType, customer);

            return (new(discountAmount, _primaryCurrency), appliedDiscount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Money ConvertRewardPointsToAmount(int rewardPoints)
            => new(ConvertRewardPointsToAmountCore(rewardPoints), _primaryCurrency);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual int ConvertAmountToRewardPoints(Money amount)
            => ConvertAmountToRewardPoints(amount.Amount);

        #region Utilities

        private readonly Func<OrganizedShoppingCartItem, CartTaxingInfo> GetTaxingInfo = cartItem
            => (CartTaxingInfo)cartItem.CustomProperties[CART_TAXING_INFO_KEY];

        private int GetTaxCategoryId(IList<OrganizedShoppingCartItem> cart, int defaultTaxCategoryId)
        {
            if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.HighestCartAmount)
            {
                return cart.FirstOrDefault(x => GetTaxingInfo(x).HasHighestCartAmount)?.Item?.Product?.TaxCategoryId ?? defaultTaxCategoryId;
            }
            else if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.HighestTaxRate)
            {
                return cart.FirstOrDefault(x => GetTaxingInfo(x).HasHighestTaxRate)?.Item?.Product?.TaxCategoryId ?? defaultTaxCategoryId;
            }

            return defaultTaxCategoryId;
        }

        protected virtual async Task<(
            decimal SubTotalWithoutDiscount,
            decimal SubTotalWithDiscount,
            decimal DiscountAmount,
            Discount AppliedDiscount,
            TaxRatesDictionary TaxRates)> GetCartSubTotalAsync(IList<OrganizedShoppingCartItem> cart, bool includeTax)
        {
            Guard.NotNull(cart, nameof(cart));

            if (!cart.Any())
            {
                return (0, 0, 0, null, null);
            }

            var customer = cart.GetCustomer();
            var subTotalExclTaxWithoutDiscount = decimal.Zero;
            var subTotalInclTaxWithoutDiscount = decimal.Zero;
            var taxRates = new TaxRatesDictionary();

            foreach (var cartItem in cart)
            {
                if (cartItem.Item.Product == null)
                {
                    continue;
                }

                var item = cartItem.Item;
                decimal taxRate, itemExclTax, itemInclTax = decimal.Zero;

                await _productAttributeMaterializer.MergeWithCombinationAsync(item.Product, item.AttributeSelection);

                if (_workingCurrency.RoundOrderItemsEnabled)
                {
                    // Gross > Net RoundFix.
                    var unitPrice2 = await _priceCalculationService.CalculateUnitPriceAsync(cartItem, false, _primaryCurrency);
                    var tax = unitPrice2.Tax.Value;

                    // Adaption to eliminate rounding issues.
                    itemExclTax = _workingCurrency.RoundIfEnabledFor(tax.PriceNet) * item.Quantity;
                    itemInclTax = _workingCurrency.RoundIfEnabledFor(tax.PriceGross) * item.Quantity;
                    taxRate = tax.Rate.Rate;
                }
                else
                {
                    var itemSubtotal = await _priceCalculationService.CalculateSubtotalAsync(cartItem, false, _primaryCurrency);
                    var tax = itemSubtotal.Tax.Value;

                    itemExclTax = _workingCurrency.RoundIfEnabledFor(tax.PriceNet);
                    itemInclTax = _workingCurrency.RoundIfEnabledFor(tax.PriceGross);
                    taxRate = tax.Rate.Rate;
                }

                subTotalExclTaxWithoutDiscount += itemExclTax;
                subTotalInclTaxWithoutDiscount += itemInclTax;

                taxRates.Add(taxRate, itemInclTax - itemExclTax);
            }

            // Checkout attributes.
            if (customer != null)
            {
                var values = await _checkoutAttributeMaterializer.MaterializeCheckoutAttributeValuesAsync(customer.GenericAttributes.CheckoutAttributes);
                foreach (var value in values)
                {
                    var (attributePriceExclTax, _) = await _taxService.GetCheckoutAttributePriceAsync(value, false, customer);
                    var (attributePriceInclTax, taxRate) = await _taxService.GetCheckoutAttributePriceAsync(value, true, customer);
                    subTotalExclTaxWithoutDiscount += attributePriceExclTax.Amount;
                    subTotalInclTaxWithoutDiscount += attributePriceInclTax.Amount;

                    taxRates.Add(taxRate, attributePriceInclTax.Amount - attributePriceExclTax.Amount);
                }
            }

            // Subtotal without discount.
            var subTotalWithoutDiscount = _workingCurrency.RoundIfEnabledFor(Math.Max(includeTax ? subTotalInclTaxWithoutDiscount : subTotalExclTaxWithoutDiscount, decimal.Zero));

            // We calculate discount amount on order subtotal excl tax (discount first).
            var (discountAmountExclTax, appliedDiscount) = await GetDiscountAmountAsync(subTotalExclTaxWithoutDiscount, DiscountType.AssignedToOrderSubTotal, customer);
            
            if (subTotalExclTaxWithoutDiscount < discountAmountExclTax)
            {
                discountAmountExclTax = subTotalExclTaxWithoutDiscount;
            }

            var discountAmountInclTax = discountAmountExclTax;

            // Subtotal with discount (excl tax).
            var subTotalExclTaxWithDiscount = subTotalExclTaxWithoutDiscount - discountAmountExclTax;
            var subTotalInclTaxWithDiscount = subTotalExclTaxWithDiscount;

            // Add tax for shopping items & checkout attributes.
            var tempTaxRates = new Dictionary<decimal, decimal>(taxRates);
            foreach (var kvp in tempTaxRates)
            {
                var taxRate = kvp.Key;
                var taxAmount = kvp.Value;

                if (taxAmount != decimal.Zero)
                {
                    // Discount the tax amount that applies to subtotal items.
                    if (subTotalExclTaxWithoutDiscount > decimal.Zero)
                    {
                        var discountTax = taxRates[taxRate] * (discountAmountExclTax / subTotalExclTaxWithoutDiscount);
                        discountAmountInclTax += discountTax;
                        taxAmount = _workingCurrency.RoundIfEnabledFor(taxRates[taxRate] - discountTax);
                        taxRates[taxRate] = taxAmount;
                    }

                    // Subtotal with discount (incl tax).
                    subTotalInclTaxWithDiscount += taxAmount;
                }
            }

            // Why no rounding of discountAmountExclTax here?
            discountAmountInclTax = _workingCurrency.RoundIfEnabledFor(discountAmountInclTax);

            var subTotalWithDiscount = _workingCurrency.RoundIfEnabledFor(Math.Max(includeTax ? subTotalInclTaxWithDiscount : subTotalExclTaxWithDiscount, decimal.Zero));

            return (
                subTotalWithoutDiscount,
                subTotalWithDiscount,
                includeTax ? discountAmountInclTax : discountAmountExclTax,
                appliedDiscount,
                taxRates);
        }

        protected virtual async Task<(
            decimal? ShippingTotal,
            Discount AppliedDiscount,
            decimal TaxRate)> GetCartShippingTotalAsync(IList<OrganizedShoppingCartItem> cart, bool includeTax)
        {
            Guard.NotNull(cart, nameof(cart));

            if (await IsFreeShippingAsync(cart))
            {
                return (decimal.Zero, null, decimal.Zero);
            }

            var (shippingTotalAmount, appliedDiscount) = await GetAdjustedShippingTotalAsync(cart);
            if (!shippingTotalAmount.HasValue)
            {
                return (null, null, decimal.Zero);
            }

            var customer = cart.GetCustomer();
            var shippingTotal = new Money(_workingCurrency.RoundIfEnabledFor(Math.Max(shippingTotalAmount.Value, decimal.Zero)), _primaryCurrency);

            await PrepareAuxiliaryServicesTaxingInfosAsync(cart);

            // Commented out because requires several plugins to be updated and migration of Order.OrderShippingTaxRate and Order.PaymentMethodAdditionalFeeTaxRate.
            //if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.ProRata)
            //{
            //	// calculate proRataShipping: get product weightings for cart and multiply them with the shipping amount
            //	shippingTotalTaxed = decimal.Zero;

            //	var tmpTaxRate = decimal.Zero;
            //	var taxRates = new List<decimal>();

            //	foreach (var item in cart)
            //	{
            //		var proRataShipping = shippingTotal.Value * GetTaxingInfo(item).ProRataWeighting;
            //		shippingTotalTaxed += _taxService.GetShippingPrice(proRataShipping, includingTax, customer, item.Item.Product.TaxCategoryId, out tmpTaxRate);

            //		taxRates.Add(tmpTaxRate);
            //	}

            //	// a tax rate is only defined if all rates are equal. return zero tax rate in all other cases.
            //	if (taxRates.Any() && taxRates.Distinct().Count() == 1)
            //	{
            //		taxRate = taxRates.First();
            //	}
            //}
            //else
            //{

            var taxCategoryId = GetTaxCategoryId(cart, _taxSettings.ShippingTaxClassId);
            var (shippingTotalTaxed, taxRate) = await _taxService.GetShippingPriceAsync(shippingTotal, includeTax, taxCategoryId, customer);

            return (shippingTotalTaxed.Amount, appliedDiscount, taxRate);
        }

        protected virtual async Task<(decimal Amount, TaxRatesDictionary TaxRates)> GetCartTaxTotalAsync(IList<OrganizedShoppingCartItem> cart, bool includePaymentFee)
        {
            Guard.NotNull(cart, nameof(cart));

            var customer = cart.GetCustomer();
            var taxRates = new TaxRatesDictionary();
            var taxTotal = decimal.Zero;
            var subTotalTax = decimal.Zero;
            var shippingTax = decimal.Zero;
            var paymentFeeTax = decimal.Zero;

            //// (VATFIX)
            if (await _taxService.IsVatExemptAsync(customer, null))
            {
                taxRates.Add(decimal.Zero, decimal.Zero);
                return (taxTotal, taxRates);
            }
            //// (VATFIX)

            // Order subtotal (cart items + checkout attributes).
            var (_, _, _, _, subTotalTaxRates) = await GetCartSubTotalAsync(cart, false);

            foreach (var pair in subTotalTaxRates)
            {
                subTotalTax += pair.Value;
                taxRates.Add(pair.Key, pair.Value);
            }

            // Shipping tax amount.
            if (_taxSettings.ShippingIsTaxable && !await IsFreeShippingAsync(cart))
            {
                var (shippingTotalAmount, _) = await GetAdjustedShippingTotalAsync(cart);
                if (shippingTotalAmount.HasValue)
                {
                    var shippingTotal = new Money(_workingCurrency.RoundIfEnabledFor(Math.Max(shippingTotalAmount.Value, decimal.Zero)), _primaryCurrency);

                    await PrepareAuxiliaryServicesTaxingInfosAsync(cart);

                    // Commented out because requires several plugins to be updated and migration of Order.OrderShippingTaxRate and Order.PaymentMethodAdditionalFeeTaxRate.
                    //if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.ProRata)
                    //{
                    //	// Calculate proRataShipping: get product weightings for cart and multiply them with the shipping amount.
                    //	foreach (var item in cart)
                    //	{
                    //		var proRataShipping = shippingTotal.Value * GetTaxingInfo(item).ProRataWeighting;
                    //		shippingTax += GetShippingTaxAmount(proRataShipping, customer, item.Item.Product.TaxCategoryId, taxRates);
                    //	}
                    //}
                    //else
                    //{

                    var taxCategoryId = GetTaxCategoryId(cart, _taxSettings.ShippingTaxClassId);
                    var (shippingExclTax, _) = await _taxService.GetShippingPriceAsync(shippingTotal, false, taxCategoryId, customer);
                    var (shippingInclTax, taxRate) = await _taxService.GetShippingPriceAsync(shippingTotal, true, taxCategoryId, customer);

                    shippingTax = _workingCurrency.RoundIfEnabledFor(Math.Max(shippingInclTax.Amount - shippingExclTax.Amount, decimal.Zero));
                    taxRates.Add(taxRate, shippingTax);
                }
            }

            // Payment fee tax amount.
            if (includePaymentFee && _taxSettings.PaymentMethodAdditionalFeeIsTaxable && customer != null)
            {
                var paymentFee = await GetShoppingCartPaymentFeeAsync(cart, customer.GenericAttributes.SelectedPaymentMethod);
                if (paymentFee != decimal.Zero)
                {
                    await PrepareAuxiliaryServicesTaxingInfosAsync(cart);

                    // Commented out because requires several plugins to be updated and migration of Order.OrderShippingTaxRate and Order.PaymentMethodAdditionalFeeTaxRate.
                    //if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.ProRata)
                    //{
                    //	// Calculate proRataShipping: get product weightings for cart and multiply them with the shipping amount.
                    //	foreach (var item in cart)
                    //	{
                    //		var proRataPaymentFees = paymentFee * GetTaxingInfo(item).ProRataWeighting;
                    //		paymentFeeTax += GetPaymentFeeTaxAmount(proRataPaymentFees, customer, item.Item.Product.TaxCategoryId, taxRates);
                    //	}
                    //}
                    //else
                    //{

                    var taxCategoryId = GetTaxCategoryId(cart, _taxSettings.PaymentMethodAdditionalFeeTaxClassId);
                    var (paymentFeeExclTax, _) = await _taxService.GetPaymentMethodFeeAsync(paymentFee, false, taxCategoryId, customer);
                    var (paymentFeeInclTax, taxRate) = await _taxService.GetPaymentMethodFeeAsync(paymentFee, true, taxCategoryId, customer);

                    // TODO: (mg) (core) rounding differences on tax amounts!
                    //var feeTax = await _taxCalculator.CalculatePaymentFeeTaxAsync(paymentFee.Amount, null, taxCategoryId, customer);
                    //paymentFeeTax = feeTax.PriceGross - feeTax.

                    // Can be less zero (taxRates code differs)!
                    paymentFeeTax = paymentFeeInclTax.Amount - paymentFeeExclTax.Amount;

                    if (taxRate > decimal.Zero && paymentFeeTax != decimal.Zero)
                    {
                        if (taxRates.ContainsKey(taxRate))
                        {
                            taxRates[taxRate] = taxRates[taxRate] + paymentFeeTax;
                        }
                        else
                        {
                            taxRates.Add(taxRate, paymentFeeTax);
                        }
                    }
                }
            }

            // Add at least one tax rate (0%).
            if (!taxRates.Any())
            {
                taxRates.Add(decimal.Zero, decimal.Zero);
            }

            taxTotal = _workingCurrency.RoundIfEnabledFor(Math.Max(subTotalTax + shippingTax + paymentFeeTax, decimal.Zero));

            return (taxTotal, taxRates);
        }

        protected virtual async Task PrepareAuxiliaryServicesTaxingInfosAsync(IList<OrganizedShoppingCartItem> cart)
        {
            // No additional infos required.
            if (!cart.Any() || _taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.SpecifiedTaxCategory)
            {
                return;
            }

            // Additional infos already collected.
            if (cart.First().CustomProperties.ContainsKey(CART_TAXING_INFO_KEY))
            {
                return;
            }

            // Instance taxing info objects.
            cart.Each(x => x.CustomProperties[CART_TAXING_INFO_KEY] = new CartTaxingInfo());

            // Collect infos.
            if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.HighestCartAmount)
            {
                // Calculate all subtotals.
                foreach (var item in cart)
                {
                    GetTaxingInfo(item).SubTotalWithoutDiscount = (await _priceCalculationService.CalculateSubtotalAsync(item, true, _primaryCurrency)).FinalPrice;
                }

                // Items with the highest subtotal.
                var highestAmountItems = cart
                    .GroupBy(x => x.Item.Product.TaxCategoryId)
                    .OrderByDescending(x => x.Sum(y => GetTaxingInfo(y).SubTotalWithoutDiscount.Amount))
                    .First();

                // Mark items.
                highestAmountItems.Each(x => GetTaxingInfo(x).HasHighestCartAmount = true);
            }
            else if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.HighestTaxRate)
            {
                var customer = cart.GetCustomer();
                var maxTaxRate = decimal.Zero;
                var maxTaxCategoryId = 0;

                // Get tax category id with the highest rate.
                foreach (var item in cart)
                {
                    var product = item.Item.Product;
                    var taxRate = await _taxService.GetTaxRateAsync(product, product.TaxCategoryId, customer);
                    if (taxRate > maxTaxRate)
                    {
                        maxTaxRate = taxRate;
                        maxTaxCategoryId = taxRate.TaxCategoryId;
                    }
                }

                // Mark items.
                cart.Where(x => x.Item.Product.TaxCategoryId == maxTaxCategoryId)
                    .Each(x => GetTaxingInfo(x).HasHighestTaxRate = true);
            }
            //else if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.ProRata)
            //{
            //	// calculate all subtotals
            //	cart.Each(x => GetTaxingInfo(x).SubTotalWithoutDiscount = _priceCalculationService.GetSubTotal(x, false));

            //	// sum over all subtotals
            //	var subTotalSum = cart.Sum(x => GetTaxingInfo(x).SubTotalWithoutDiscount);

            //	// calculate pro rata weightings
            //	cart.Each(x =>
            //	{
            //		var taxingInfo = GetTaxingInfo(x);
            //		taxingInfo.ProRataWeighting = taxingInfo.SubTotalWithoutDiscount / subTotalSum;
            //	});
            //}
        }

        protected virtual async Task<(decimal Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(decimal amount, DiscountType discountType, Customer customer)
        {
            var result = decimal.Zero;
            Discount appliedDiscount = null;

            if (!_catalogSettings.IgnoreDiscounts)
            {
                var allowedDiscounts = new List<Discount>();
                var allDiscounts = await _discountService.GetAllDiscountsAsync(discountType);

                foreach (var discount in allDiscounts)
                {
                    if (discount.DiscountType == discountType &&
                        !allowedDiscounts.Any(x => x.Id == discount.Id) &&
                        await _discountService.IsDiscountValidAsync(discount, customer))
                    {
                        allowedDiscounts.Add(discount);
                    }
                }

                appliedDiscount = allowedDiscounts.GetPreferredDiscount(amount);
                if (appliedDiscount != null)
                {
                    result = appliedDiscount.GetDiscountAmount(amount);
                }
            }

            if (result < decimal.Zero)
            {
                result = decimal.Zero;
            }

            if (discountType != DiscountType.AssignedToOrderSubTotal)
            {
                result = _workingCurrency.RoundIfEnabledFor(result);
            }

            return (result, appliedDiscount);
        }

        protected virtual async Task<decimal> GetShippingChargeAsync(IList<OrganizedShoppingCartItem> cart)
        {
            var charge = decimal.Zero;

            if (!await IsFreeShippingAsync(cart))
            {
                foreach (var cartItem in cart)
                {
                    var item = cartItem.Item;

                    if (_shippingSettings.ChargeOnlyHighestProductShippingSurcharge)
                    {
                        if (charge < item.Product.AdditionalShippingCharge)
                        {
                            charge = item.Product.AdditionalShippingCharge;
                        }
                    }
                    else
                    {
                        if (item.IsShippingEnabled && !item.IsFreeShipping && item.Product != null)
                        {
                            if (item.Product.ProductType == ProductType.BundledProduct && item.Product.BundlePerItemShipping)
                            {
                                cartItem.ChildItems.Each(x => charge += x.Item.Product.AdditionalShippingCharge * x.Item.Quantity);
                            }
                            else
                            {
                                charge += item.Product.AdditionalShippingCharge * item.Quantity;
                            }
                        }
                    }
                }
            }

            return charge;
        }

        protected virtual async Task<(decimal Amount, Discount AppliedDiscount)> AdjustShippingRateAsync(
            IList<OrganizedShoppingCartItem> cart,
            decimal shippingRate,
            ShippingOption shippingOption,
            IList<ShippingMethod> shippingMethods)
        {
            Guard.NotNull(cart, nameof(cart));

            if (await IsFreeShippingAsync(cart))
            {
                return (decimal.Zero, null);
            }

            var customer = cart.GetCustomer();
            var ignoreAdditionalShippingCharge = false;
            var bundlePerItemShipping = decimal.Zero;
            var adjustedRate = decimal.Zero;

            foreach (var cartItem in cart)
            {
                var item = cartItem.Item;

                if (item.Product != null && item.Product.ProductType == ProductType.BundledProduct && item.Product.BundlePerItemShipping)
                {
                    foreach (var childItem in cartItem.ChildItems.Where(x => x.Item.IsShippingEnabled && !x.Item.IsFreeShipping))
                    {
                        bundlePerItemShipping += shippingRate;
                    }
                }
                else if (adjustedRate == decimal.Zero)
                {
                    adjustedRate = shippingRate;
                }
            }

            adjustedRate += bundlePerItemShipping;

            if (shippingOption != null && shippingMethods != null)
            {
                var shippingMethod = shippingMethods.FirstOrDefault(x => x.Id == shippingOption.ShippingMethodId);
                if (shippingMethod != null)
                {
                    ignoreAdditionalShippingCharge = shippingMethod.IgnoreCharges;
                }
            }

            // Additional shipping charges.
            if (!ignoreAdditionalShippingCharge)
            {
                var additionalShippingCharge = await GetShippingChargeAsync(cart);
                adjustedRate += additionalShippingCharge;
            }

            // Discount.
            var (discountAmount, discount) = await GetDiscountAmountAsync(adjustedRate, DiscountType.AssignedToShipping, customer);
            adjustedRate = _workingCurrency.RoundIfEnabledFor(Math.Max(adjustedRate - discountAmount, decimal.Zero));

            return (adjustedRate, discount);
        }

        protected virtual async Task<(decimal? Amount, Discount AppliedDiscount)> GetAdjustedShippingTotalAsync(IList<OrganizedShoppingCartItem> cart)
        {
            var storeId = _storeContext.CurrentStore.Id;
            var customer = cart.GetCustomer();
            var shippingOption = customer?.GenericAttributes?.SelectedShippingOption?.Convert<ShippingOption>();

            if (shippingOption != null)
            {
                // Use last shipping option (get from cache).
                var shippingMethods = await _shippingService.GetAllShippingMethodsAsync(false, storeId);

                return await AdjustShippingRateAsync(cart, shippingOption.Rate, shippingOption, shippingMethods);
            }
            else
            {
                // Use fixed rate (if possible).
                var shippingAddress = customer?.ShippingAddress ?? null;
                var shippingRateComputationMethods = _shippingService.LoadActiveShippingRateComputationMethods(storeId);

                if (!shippingRateComputationMethods.Any())
                {
                    throw new SmartException(T("Shipping.CouldNotLoadMethod"));
                }

                if (shippingRateComputationMethods.Count() == 1)
                {
                    var shippingRateComputationMethod = shippingRateComputationMethods.First();
                    var getShippingOptionRequest = _shippingService.CreateShippingOptionRequest(cart, shippingAddress, storeId);
                    var fixedRate = shippingRateComputationMethod.Value.GetFixedRate(getShippingOptionRequest);

                    if (fixedRate.HasValue)
                    {
                        // Ignore returned currency. The caller specifies it to avoid mixed currencies during calculation.
                        return await AdjustShippingRateAsync(cart, fixedRate.Value.Amount, null, null);
                    }
                }
            }

            return (null, null);
        }

        protected virtual async Task<decimal> GetCartPaymentFeeAsync(IList<OrganizedShoppingCartItem> cart, string paymentMethodSystemName)
        {
            Guard.NotNull(cart, nameof(cart));

            var paymentFee = decimal.Zero;
            var provider = _providerManager.GetProvider<IPaymentMethod>(paymentMethodSystemName);
            if (provider != null)
            {
                var (fixedFeeOrPercentage, usePercentage) = await provider.Value.GetPaymentFeeInfoAsync(cart);
                if (fixedFeeOrPercentage != decimal.Zero)
                {
                    if (usePercentage)
                    {
                        // Percentage.
                        Money? orderTotalWithoutPaymentFee = await GetShoppingCartTotalAsync(cart, includePaymentFee: false);
                        if (orderTotalWithoutPaymentFee.HasValue)
                        {
                            paymentFee = orderTotalWithoutPaymentFee.Value.Amount * fixedFeeOrPercentage / 100m;
                        }
                    }
                    else
                    {
                        // Fixed fee value.
                        paymentFee = fixedFeeOrPercentage;
                    }
                }
            }

            return _workingCurrency.RoundIfEnabledFor(paymentFee);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual decimal ConvertRewardPointsToAmountCore(int rewardPoints)
            => _workingCurrency.RoundIfEnabledFor(rewardPoints > 0 ? rewardPoints * _rewardPointsSettings.ExchangeRate : decimal.Zero);

        protected virtual int ConvertAmountToRewardPoints(decimal amount)
        {
            if (amount <= 0 || _rewardPointsSettings.ExchangeRate <= 0)
            {
                return 0;
            }

            return _rewardPointsSettings.RoundDownRewardPoints
                ? (int)Math.Floor(amount / _rewardPointsSettings.ExchangeRate)
                : (int)Math.Ceiling(amount / _rewardPointsSettings.ExchangeRate);
        }

        #endregion
    }

    internal class CartTaxingInfo
    {
        public Money SubTotalWithoutDiscount { get; internal set; }
        public bool HasHighestCartAmount { get; internal set; }
        public bool HasHighestTaxRate { get; internal set; }
        public decimal ProRataWeighting { get; internal set; } = decimal.Zero;
    }
}
