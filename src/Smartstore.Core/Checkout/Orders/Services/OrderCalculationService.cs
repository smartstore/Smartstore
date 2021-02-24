using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
using Smartstore.Core.Domain.Catalog;
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
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IDiscountService _discountService;
        private readonly IShippingService _shippingService;
        private readonly IGiftCardService _giftCardService;
        private readonly ICurrencyService _currencyService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        private readonly IProviderManager _providerManager;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext; 
        private readonly ITaxService _taxService;
        private readonly TaxSettings _taxSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly ShippingSettings _shippingSettings;

        public OrderCalculationService(
            SmartDbContext db,
            IPriceCalculationService priceCalculationService,
            IDiscountService discountService,
            IShippingService shippingService,
            IGiftCardService giftCardService,
            ICurrencyService currencyService,
            IProductAttributeMaterializer productAttributeMaterializer,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            IProviderManager providerManager,
            IWorkContext workContext,
            IStoreContext storeContext,
            ITaxService taxService,
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
            _productAttributeMaterializer = productAttributeMaterializer;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            _providerManager = providerManager;
            _workContext = workContext;
            _storeContext = storeContext;
            _taxService = taxService;
            _taxSettings = taxSettings;
            _rewardPointsSettings = rewardPointsSettings;
            _catalogSettings = catalogSettings;
            _shippingSettings = shippingSettings;
        }        

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual async Task<ShoppingCartTotal> GetShoppingCartTotalAsync(
            IList<OrganizedShoppingCartItem> cart,
            bool includeRewardPoints = true,
            bool includePaymentAdditionalFee = true,
            bool includeCreditBalance = true)
        {
            Guard.NotNull(cart, nameof(cart));

            var store = _storeContext.CurrentStore;
            var currency = _workContext.WorkingCurrency;
            var customer = cart.GetCustomer();

            var paymentMethodSystemName = customer != null
                ? customer.GenericAttributes.SelectedPaymentMethod
                : string.Empty;

            var subTotal = await GetShoppingCartSubTotalAsync(cart, false);

            // Subtotal with discount.
            var subtotalBase = subTotal.SubTotalWithDiscount;

            // Shipping without tax.
            Money? shoppingCartShipping = await GetShoppingCartShippingTotalAsync(cart, false);

            // Payment method additional fee without tax.
            var paymentFeeWithoutTax = decimal.Zero;
            if (includePaymentAdditionalFee && paymentMethodSystemName.HasValue())
            {
                var provider = _providerManager.GetProvider<IPaymentMethod>(paymentMethodSystemName);
                var paymentMethodAdditionalFee = await provider?.Value?.GetAdditionalHandlingFeeAsync(cart);

                // TODO: (ms) (core) Money struct
                paymentFeeWithoutTax = await _taxService.GetPaymentMethodAdditionalFeeAsync(paymentMethodAdditionalFee.Amount, false, customer: customer);
            }

            // Tax
            var (shoppingCartTax, _) = await GetTaxTotalAsync(cart, includePaymentAdditionalFee);

            // Order total.
            var resultTemp = subtotalBase.Amount;

            if (shoppingCartShipping.HasValue)
            {
                resultTemp += shoppingCartShipping.Value.Amount;
            }

            resultTemp += paymentFeeWithoutTax;
            resultTemp += shoppingCartTax;
            resultTemp = currency.RoundIfEnabledFor(resultTemp);

            // Order total discount.
            var (discountAmount, appliedDiscount) = await this.GetOrderTotalDiscountAsync(resultTemp, customer);

            // Subtotal with discount.
            if (resultTemp < discountAmount)
            {
                discountAmount = resultTemp;
            }

            // Reduce subtotal.
            resultTemp = currency.RoundIfEnabledFor(Math.Max(resultTemp - discountAmount, decimal.Zero));

            // Applied gift cards.
            var appliedGiftCards = new List<AppliedGiftCard>();
            if (!cart.IncludesMatchingItems(x => x.IsRecurring))
            {
                var giftCards = await _giftCardService.GetValidGiftCardsAsync(store.Id, customer);
                foreach (var gc in giftCards)
                {
                    if (resultTemp > decimal.Zero)
                    {
                        var usableAmount = resultTemp > gc.UsableAmount ? gc.UsableAmount : resultTemp;

                        // Reduce subtotal.
                        resultTemp -= usableAmount;

                        appliedGiftCards.Add(new()
                        {
                            GiftCard = gc.GiftCard,
                            UsableAmount = usableAmount
                        });
                    }
                }
            }

            // Reward points.
            var redeemedRewardPoints = 0;
            var redeemedRewardPointsAmount = decimal.Zero;

            if (includeRewardPoints && resultTemp > decimal.Zero && _rewardPointsSettings.Enabled && customer != null &&
                customer.GenericAttributes.UseRewardPointsDuringCheckout)
            {
                var rewardPointsBalance = customer.GetRewardPointsBalance();
                var rewardPointsBalanceAmount = ConvertRewardPointsToAmount(rewardPointsBalance);

                if (resultTemp > rewardPointsBalanceAmount)
                {
                    redeemedRewardPoints = rewardPointsBalance;
                    redeemedRewardPointsAmount = rewardPointsBalanceAmount;
                }
                else
                {
                    redeemedRewardPoints = ConvertAmountToRewardPoints(redeemedRewardPointsAmount);
                    redeemedRewardPointsAmount = resultTemp;
                }
            }

            resultTemp = currency.RoundIfEnabledFor(Math.Max(resultTemp, decimal.Zero));

            // Return null if we have errors:
            var orderTotal = shoppingCartShipping.HasValue ? resultTemp : (decimal?)null;
            var orderTotalConverted = orderTotal;
            var roundingAmount = decimal.Zero;
            var roundingAmountConverted = decimal.Zero;
            var appliedCreditBalance = decimal.Zero;

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

                orderTotal = currency.RoundIfEnabledFor(orderTotal.Value - appliedCreditBalance);

                orderTotalConverted = _currencyService.ConvertFromPrimaryStoreCurrency(orderTotal.Value, currency, store);

                // Order total rounding.
                if (currency.RoundOrderTotalEnabled && paymentMethodSystemName.HasValue())
                {
                    var paymentMethod = await _db.PaymentMethods.AsNoTracking().FirstOrDefaultAsync(x => x.PaymentMethodSystemName == paymentMethodSystemName);
                    if (paymentMethod?.RoundOrderTotalEnabled ?? false)
                    {
                        orderTotal = currency.RoundToNearest(orderTotal.Value, out roundingAmount);
                        orderTotalConverted = currency.RoundToNearest(orderTotalConverted.Value, out roundingAmountConverted);
                    }
                }
            }

            var result = new ShoppingCartTotal(orderTotal)
            {
                RoundingAmount = roundingAmount,
                DiscountAmount = discountAmount,
                AppliedDiscount = appliedDiscount,
                AppliedGiftCards = appliedGiftCards,
                RedeemedRewardPoints = redeemedRewardPoints,
                RedeemedRewardPointsAmount = redeemedRewardPointsAmount,
                CreditBalance = appliedCreditBalance,
                ConvertedAmount = new ShoppingCartTotal.ConvertedAmounts
                {
                    TotalAmount = orderTotalConverted,
                    RoundingAmount = roundingAmountConverted
                }
            };

            return result;
        }

        public virtual async Task<ShoppingCartSubTotal> GetShoppingCartSubTotalAsync(IList<OrganizedShoppingCartItem> cart, bool? includeTax = null)
        {
            Guard.NotNull(cart, nameof(cart));

            var includingTax = includeTax ?? (_workContext.TaxDisplayType == TaxDisplayType.IncludingTax);
            var result = new ShoppingCartSubTotal();

            if (!(cart?.Any() ?? false))
            {
                return result;
            }

            var subTotalExclTaxWithoutDiscount = decimal.Zero;
            var subTotalInclTaxWithoutDiscount = decimal.Zero;
            var currency = _workContext.WorkingCurrency;
            var customer = cart.GetCustomer();

            foreach (var cartItem in cart)
            {
                if (cartItem.Item.Product == null)
                {
                    continue;
                }

                var item = cartItem.Item;
                decimal itemSubTotal, itemExclTax, itemInclTax = decimal.Zero;

                await _productAttributeMaterializer.MergeWithCombinationAsync(item.Product, item.AttributeSelection);

                if (currency.RoundOrderItemsEnabled)
                {
                    // Gross > Net RoundFix.
                    itemSubTotal = await _priceCalculationService.GetUnitPriceAsync(cartItem, true);

                    // Adaption to eliminate rounding issues.
                    itemExclTax = await _taxService.GetProductPriceAsync(item.Product, itemSubTotal, false, customer: customer);
                    itemExclTax = currency.RoundIfEnabledFor(itemExclTax) * item.Quantity;
                    itemInclTax = await _taxService.GetProductPriceAsync(item.Product, itemSubTotal, true, customer: customer);
                    itemInclTax = currency.RoundIfEnabledFor(itemInclTax) * item.Quantity;
                }
                else
                {
                    itemSubTotal = await _priceCalculationService.GetSubTotalAsync(cartItem, true);
                    itemExclTax = await _taxService.GetProductPriceAsync(item.Product, itemSubTotal, false, customer: customer);
                    itemInclTax = await _taxService.GetProductPriceAsync(item.Product, itemSubTotal, true, customer: customer);
                }

                subTotalExclTaxWithoutDiscount += itemExclTax;
                subTotalInclTaxWithoutDiscount += itemInclTax;

                // TODO: (ms) (core) TaxService.Get<Product|Shipping|CheckoutAttribute|...><Price|Fee>Async must provide the applied tax rate.
                var taxRate = decimal.Zero;
                result.TaxRates.Add(taxRate, itemInclTax - itemExclTax);
            }

            // Checkout attributes.
            if (customer != null)
            {
                var values = await _checkoutAttributeMaterializer.MaterializeCheckoutAttributeValuesAsync(customer.GenericAttributes.CheckoutAttributes);
                foreach (var value in values)
                {
                    var attributePriceExclTax = await _taxService.GetCheckoutAttributePriceAsync(value, customer, false);
                    var attributePriceInclTax = await _taxService.GetCheckoutAttributePriceAsync(value, customer, true);
                    subTotalExclTaxWithoutDiscount += attributePriceExclTax;
                    subTotalInclTaxWithoutDiscount += attributePriceInclTax;

                    // TODO: (ms) (core) TaxService.Get<Product|Shipping|CheckoutAttribute|...><Price|Fee>Async must provide the applied tax rate.
                    var taxRate = decimal.Zero;
                    result.TaxRates.Add(taxRate, attributePriceInclTax - attributePriceExclTax);
                }
            }

            // Subtotal without discount.
            result.SubTotalWithoutDiscount = includingTax
                ? currency.AsMoney(Math.Max(subTotalInclTaxWithoutDiscount, decimal.Zero))
                : currency.AsMoney(Math.Max(subTotalExclTaxWithoutDiscount, decimal.Zero));

            // We calculate discount amount on order subtotal excl tax (discount first).
            var (discountAmountExclTax, appliedDiscount) = await this.GetOrderSubtotalDiscountAsync(subTotalExclTaxWithoutDiscount, customer);
            result.AppliedDiscount = appliedDiscount;

            if (subTotalExclTaxWithoutDiscount < discountAmountExclTax)
            {
                discountAmountExclTax = subTotalExclTaxWithoutDiscount;
            }

            var discountAmountInclTax = discountAmountExclTax;

            // Subtotal with discount (excl tax).
            var subTotalExclTaxWithDiscount = subTotalExclTaxWithoutDiscount - discountAmountExclTax;
            var subTotalInclTaxWithDiscount = subTotalExclTaxWithDiscount;

            // Add tax for shopping items & checkout attributes.
            var tempTaxRates = new Dictionary<decimal, decimal>(result.TaxRates);
            foreach (var kvp in tempTaxRates)
            {
                var taxRate = kvp.Key;
                var taxAmount = kvp.Value;

                if (taxAmount != decimal.Zero)
                {
                    // Discount the tax amount that applies to subtotal items.
                    if (subTotalExclTaxWithoutDiscount > decimal.Zero)
                    {
                        var discountTax = result.TaxRates[taxRate] * (discountAmountExclTax / subTotalExclTaxWithoutDiscount);
                        discountAmountInclTax += discountTax;
                        taxAmount = currency.RoundIfEnabledFor(result.TaxRates[taxRate] - discountTax);
                        result.TaxRates[taxRate] = taxAmount;
                    }

                    // Subtotal with discount (incl tax).
                    subTotalInclTaxWithDiscount += taxAmount;
                }
            }

            discountAmountInclTax = currency.RoundIfEnabledFor(discountAmountInclTax);

            result.DiscountAmount = currency.AsMoney(includingTax ? discountAmountInclTax : discountAmountExclTax);

            result.SubTotalWithDiscount = currency.AsMoney(Math.Max(includingTax ? subTotalInclTaxWithDiscount : subTotalExclTaxWithDiscount, decimal.Zero));

            return result;
        }

        public virtual async Task<ShoppingCartShippingTotal> GetShoppingCartShippingTotalAsync(IList<OrganizedShoppingCartItem> cart, bool? includeTax = null)
        {
            Guard.NotNull(cart, nameof(cart));

            var includingTax = includeTax ?? (_workContext.TaxDisplayType == TaxDisplayType.IncludingTax);
            var currency = _workContext.WorkingCurrency;
            var customer = cart.GetCustomer();

            if (await IsFreeShippingAsync(cart))
            {
                return new ShoppingCartShippingTotal(currency.AsMoney(decimal.Zero));
            }

            var (shippingTotal, appliedDiscount) = await GetAdjustedShippingTotalAsync(cart);
            if (!shippingTotal.HasValue)
            {
                return new ShoppingCartShippingTotal(null);
            }

            shippingTotal = currency.RoundIfEnabledFor(Math.Max(shippingTotal.Value, decimal.Zero));

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

            // TODO: (ms) (core) TaxService.Get<Product|Shipping|CheckoutAttribute|...><Price|Fee>Async must provide the applied tax rate.
            var taxRate = decimal.Zero;
            var shippingTotalTaxed = await _taxService.GetShippingPriceAsync(shippingTotal.Value, includingTax, customer, taxCategoryId);

            return new ShoppingCartShippingTotal(currency.AsMoney(shippingTotalTaxed, true))
            {
                AppliedDiscount = appliedDiscount,
                TaxRate = taxRate
            };
        }

        public virtual async Task<(decimal Amount, TaxRatesDictionary taxRates)> GetTaxTotalAsync(IList<OrganizedShoppingCartItem> cart, bool includePaymentAdditionalFee = true)
        {
            Guard.NotNull(cart, nameof(cart));

            var taxTotal = decimal.Zero;
            var taxRates = new TaxRatesDictionary();
            var currency = _workContext.WorkingCurrency;
            var customer = cart.GetCustomer();
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
            var subTotal = await GetShoppingCartSubTotalAsync(cart, false);

            foreach (KeyValuePair<decimal, decimal> kvp in subTotal.TaxRates)
            {
                subTotalTax += kvp.Value;
                taxRates.Add(kvp.Key, kvp.Value);
            }

            // Shipping tax amount.
            if (_taxSettings.ShippingIsTaxable && !await IsFreeShippingAsync(cart))
            {
                var (shippingTotal, _) = await GetAdjustedShippingTotalAsync(cart);

                if (shippingTotal.HasValue)
                {
                    shippingTotal = currency.RoundIfEnabledFor(Math.Max(shippingTotal.Value, decimal.Zero));

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

                    shippingTax = await GetShippingTaxAmountAsync(shippingTotal.Value, customer, taxCategoryId, taxRates);
                    shippingTax = currency.RoundIfEnabledFor(shippingTax);
                }
            }

            // Payment fee tax amount.
            if (includePaymentAdditionalFee && _taxSettings.PaymentMethodAdditionalFeeIsTaxable && customer != null)
            {
                var paymentMethodSystemName = customer.GenericAttributes.SelectedPaymentMethod;
                var provider = _providerManager.GetProvider<IPaymentMethod>(paymentMethodSystemName);

                if (provider != null)
                {
                    var paymentFee = await provider.Value.GetAdditionalHandlingFeeAsync(cart);

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

                    // TODO: (ms) (core) Money struct
                    paymentFeeTax = await GetPaymentFeeTaxAmountAsync(paymentFee.Amount, customer, taxCategoryId, taxRates);
                }
            }

            // Add at least one tax rate (0%).
            if (!taxRates.Any())
            {
                taxRates.Add(decimal.Zero, decimal.Zero);
            }

            taxTotal = currency.RoundIfEnabledFor(Math.Max(subTotalTax + shippingTax + paymentFeeTax, decimal.Zero));

            return (taxTotal, taxRates);
        }

        public virtual async Task<bool> IsFreeShippingAsync(IList<OrganizedShoppingCartItem> cart)
        {
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
                Money subTotalWithDiscount = await GetShoppingCartSubTotalAsync(cart, _shippingSettings.FreeShippingOverXIncludingTax);
                if (subTotalWithDiscount > _shippingSettings.FreeShippingOverXValue)
                {
                    return true;
                }
            }

            return false;
        }

        public virtual async Task<decimal> GetShoppingCartAdditionalShippingChargeAsync(IList<OrganizedShoppingCartItem> cart)
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

        public virtual async Task<(decimal Amount, Discount AppliedDiscount)> AdjustShippingRateAsync(
            IList<OrganizedShoppingCartItem> cart,
            decimal shippingRate,
            ShippingOption shippingOption,
            IList<ShippingMethod> shippingMethods)
        {
            if (await IsFreeShippingAsync(cart))
            {
                return (decimal.Zero, null);
            }

            var adjustedRate = decimal.Zero;
            var bundlePerItemShipping = decimal.Zero;
            var ignoreAdditionalShippingCharge = false;

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
                var additionalShippingCharge = await GetShoppingCartAdditionalShippingChargeAsync(cart);
                adjustedRate += additionalShippingCharge;
            }

            // Discount.
            var (discountAmount, discount) = await this.GetShippingDiscountAsync(adjustedRate, cart.GetCustomer());

            adjustedRate = _workContext.WorkingCurrency.RoundIfEnabledFor(Math.Max(adjustedRate - discountAmount, decimal.Zero));

            return (adjustedRate, discount);
        }

        public virtual async Task<(decimal Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(decimal amount, DiscountType discountType, Customer customer, bool round = true)
        {
            var discountAmount = decimal.Zero;
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
                    discountAmount = Math.Max(appliedDiscount.GetDiscountAmount(amount), decimal.Zero);
                    if (round)
                    {
                        discountAmount = _workContext.WorkingCurrency.RoundIfEnabledFor(discountAmount);
                    }
                }
            }

            return (discountAmount, appliedDiscount);
        }

        public virtual decimal ConvertRewardPointsToAmount(int rewardPoints)
        {
            if (rewardPoints > 0)
            {
                return _workContext.WorkingCurrency.RoundIfEnabledFor(rewardPoints * _rewardPointsSettings.ExchangeRate);
            }

            return decimal.Zero;
        }

        public virtual int ConvertAmountToRewardPoints(decimal amount)
        {
            if (amount > 0 && _rewardPointsSettings.ExchangeRate > 0)
            {
                return _rewardPointsSettings.RoundDownRewardPoints
                    ? (int)Math.Floor(amount / _rewardPointsSettings.ExchangeRate)
                    : (int)Math.Ceiling(amount / _rewardPointsSettings.ExchangeRate);
            }

            return 0;
        }

        #region Utilities

        private readonly Func<OrganizedShoppingCartItem, CartTaxingInfo> GetTaxingInfo = cartItem => (CartTaxingInfo)cartItem.CustomProperties[CART_TAXING_INFO_KEY];

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
                    GetTaxingInfo(item).SubTotalWithoutDiscount = await _priceCalculationService.GetSubTotalAsync(item, false);
                }

                // Items with the highest subtotal.
                var highestAmountItems = cart
                    .GroupBy(x => x.Item.Product.TaxCategoryId)
                    .OrderByDescending(x => x.Sum(y => GetTaxingInfo(y).SubTotalWithoutDiscount))
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
                        maxTaxCategoryId = product.TaxCategoryId;
                    }
                }
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

        protected virtual async Task<(decimal? Amount, Discount AppliedDiscount)> GetAdjustedShippingTotalAsync(IList<OrganizedShoppingCartItem> cart)
        {
            var customer = cart.GetCustomer();
            var storeId = _storeContext.CurrentStore.Id;

            var shippingOption = customer != null
                ? customer.GenericAttributes.SelectedShippingOption?.Convert<ShippingOption>()
                : null;

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
                        return await AdjustShippingRateAsync(cart, fixedRate.Value, null, null);
                    }
                }
            }

            return (null, null);
        }

        protected virtual async Task<decimal> GetShippingTaxAmountAsync(
            decimal shipping,
            Customer customer,
            int taxCategoryId,
            SortedDictionary<decimal, decimal> taxRates)
        {
            // TODO: (ms) (core) TaxService.Get<Product|Shipping|CheckoutAttribute><Price|Fee|...>Async must provide the applied tax rate.
            // Just calling GetTaxRateAsync would result in a bug. The caller should not be forced to find out the correct rate by handling TaxSettings.ShippingIsTaxable, TaxSettings.ShippingTaxClassId etc.
            var taxRate = decimal.Zero;
            var shippingExclTax = await _taxService.GetShippingPriceAsync(shipping, false, customer, taxCategoryId);
            var shippingInclTax = await _taxService.GetShippingPriceAsync(shipping, true, customer, taxCategoryId);
            var shippingTax = shippingInclTax - shippingExclTax;

            if (shippingTax < decimal.Zero)
            {
                shippingTax = decimal.Zero;
            }

            if (taxRate > decimal.Zero && shippingTax > decimal.Zero)
            {
                if (taxRates.ContainsKey(taxRate))
                {
                    taxRates[taxRate] = taxRates[taxRate] + shippingTax;
                }
                else
                {
                    taxRates.Add(taxRate, shippingTax);
                }
            }

            return shippingTax;
        }

        protected virtual async Task<decimal> GetPaymentFeeTaxAmountAsync(
            decimal paymentFee,
            Customer customer,
            int taxCategoryId,
            SortedDictionary<decimal, decimal> taxRates)
        {
            // TODO: (ms) (core) TaxService.Get<Product|Shipping|CheckoutAttribute|...><Price|Fee>Async must provide the applied tax rate.
            var taxRate = decimal.Zero;
            var paymentFeeExclTax = await _taxService.GetPaymentMethodAdditionalFeeAsync(paymentFee, false, taxCategoryId, customer);
            var paymentFeeInclTax = await _taxService.GetPaymentMethodAdditionalFeeAsync(paymentFee, true, taxCategoryId, customer);
            var paymentFeeTax = paymentFeeInclTax - paymentFeeExclTax;

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

            return paymentFeeTax;
        }

        #endregion
    }

    internal class CartTaxingInfo
    {
        public decimal SubTotalWithoutDiscount { get; internal set; }
        public bool HasHighestCartAmount { get; internal set; }
        public bool HasHighestTaxRate { get; internal set; }
        public decimal ProRataWeighting { get; internal set; } = decimal.Zero;
    }
}
