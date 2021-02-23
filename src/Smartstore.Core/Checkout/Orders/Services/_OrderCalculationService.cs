using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class OrderCalculationService : IOrderCalculationService
    {
        private const string CART_TAXING_INFO_KEY = "CartTaxingInfos";

        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IDiscountService _discountService;
        private readonly IShippingService _shippingService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext; 
        private readonly ITaxService _taxService;
        private readonly TaxSettings _taxSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly ShippingSettings _shippingSettings;

        public OrderCalculationService(
            IPriceCalculationService priceCalculationService,
            IDiscountService discountService,
            IShippingService shippingService,
            IProductAttributeMaterializer productAttributeMaterializer,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            IWorkContext workContext,
            IStoreContext storeContext,
            ITaxService taxService,
            TaxSettings taxSettings,
            RewardPointsSettings rewardPointsSettings,
            CatalogSettings catalogSettings,
            ShippingSettings shippingSettings)
        {
            _priceCalculationService = priceCalculationService;
            _discountService = discountService;
            _shippingService = shippingService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            _workContext = workContext;
            _storeContext = storeContext;
            _taxService = taxService;
            _taxSettings = taxSettings;
            _rewardPointsSettings = rewardPointsSettings;
            _catalogSettings = catalogSettings;
            _shippingSettings = shippingSettings;
        }        

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual async Task<ShoppingCartSubTotal> GetShoppingCartSubTotalAsync(IList<OrganizedShoppingCartItem> cart, bool? includingTax = null)
        {
            Guard.NotNull(cart, nameof(cart));

            var includeTax = includingTax ?? (_workContext.TaxDisplayType == TaxDisplayType.IncludingTax);
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

                // TODO: (mg) (core) TaxService.GetTaxRateAsync usage is wrong. Get from TaxService.GetProductPriceAsync.
                var taxRate = decimal.Zero;
                result.AddTaxRate(taxRate, itemInclTax - itemExclTax);
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

                    // TODO: (mg) (core) TaxService.GetTaxRateAsync usage is wrong. Get rate from GetCheckoutAttributePrice.
                    var taxRate = decimal.Zero;
                    result.AddTaxRate(taxRate, attributePriceInclTax - attributePriceExclTax);
                }
            }

            // Subtotal without discount.
            result.SubTotalWithoutDiscount = includeTax
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
                var taxValue = kvp.Value;

                if (taxValue != decimal.Zero)
                {
                    // Discount the tax amount that applies to subtotal items.
                    if (subTotalExclTaxWithoutDiscount > decimal.Zero)
                    {
                        var discountTax = result.TaxRates[taxRate] * (discountAmountExclTax / subTotalExclTaxWithoutDiscount);
                        discountAmountInclTax += discountTax;
                        taxValue = currency.RoundIfEnabledFor(result.TaxRates[taxRate] - discountTax);
                        result.TaxRates[taxRate] = taxValue;
                    }

                    // Subtotal with discount (incl tax).
                    subTotalInclTaxWithDiscount += taxValue;
                }
            }

            discountAmountInclTax = currency.RoundIfEnabledFor(discountAmountInclTax);

            result.DiscountAmount = currency.AsMoney(includeTax ? discountAmountInclTax : discountAmountExclTax);

            result.SubTotalWithDiscount = currency.AsMoney(Math.Max(includeTax ? subTotalInclTaxWithDiscount : subTotalExclTaxWithDiscount, decimal.Zero));

            return result;
        }

        public virtual async Task<ShoppingCartShippingTotal> GetShoppingCartShippingTotalAsync(IList<OrganizedShoppingCartItem> cart, bool? includingTax = null)
        {
            Guard.NotNull(cart, nameof(cart));

            var includeTax = includingTax ?? (_workContext.TaxDisplayType == TaxDisplayType.IncludingTax);
            var currency = _workContext.WorkingCurrency;
            var customer = cart.GetCustomer();
            var taxCategoryId = 0;

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

            if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.HighestCartAmount)
            {
                taxCategoryId = cart.FirstOrDefault(x => GetTaxingInfo(x).HasHighestCartAmount)?.Item?.Product?.TaxCategoryId ?? 0;
            }
            else if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.HighestTaxRate)
            {
                taxCategoryId = cart.FirstOrDefault(x => GetTaxingInfo(x).HasHighestTaxRate)?.Item?.Product?.TaxCategoryId ?? 0;
            }

            // Fallback to setting.
            if (taxCategoryId == 0)
            {
                taxCategoryId = _taxSettings.ShippingTaxClassId;
            }

            // TODO: (mg) (core) TaxService.GetTaxRateAsync usage is wrong. Get from TaxService.GetShippingPriceAsync.
            var taxRate = decimal.Zero;
            var shippingTotalTaxed = await _taxService.GetShippingPriceAsync(shippingTotal.Value, includeTax, customer, taxCategoryId);

            return new ShoppingCartShippingTotal(currency.AsMoney(shippingTotalTaxed, true))
            {
                AppliedDiscount = appliedDiscount,
                TaxRate = taxRate
            };
        }

        public virtual Task<bool> IsFreeShippingAsync(IList<OrganizedShoppingCartItem> cart)
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
                    return Task.FromResult(true);
                }
            }

            if (!cart.IsShippingRequired())
            {
                return Task.FromResult(true);
            }

            // Check whether there is at least one item with chargeable shipping.
            if (!cart.Any(x => x.Item.IsShippingEnabled && !x.Item.IsFreeShipping))
            {
                return Task.FromResult(true);
            }

            // Check if the subtotal is large enough for free shipping.
            if (_shippingSettings.FreeShippingOverXEnabled)
            {
                // TODO: (mg) (core) Complete OrderTotalCalculationService.IsFreeShippingAsync.
                //GetShoppingCartSubTotal(cart, _shippingSettings.FreeShippingOverXIncludingTax, out subTotalDiscountAmount, out subTotalAppliedDiscount, out subTotalWithoutDiscountBase, out subTotalWithDiscountBase);
                var subTotalWithDiscountBase = decimal.Zero;
                if (subTotalWithDiscountBase > _shippingSettings.FreeShippingOverXValue)
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
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
                ? customer.GenericAttributes.Get<ShippingOption>(SystemCustomerAttributeNames.SelectedShippingOption, storeId)
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
            // TODO: (mg) (core) TaxService.GetTaxRateAsync usage is wrong. Get rate from GetShippingPriceAsync.
            //var taxRate = await _taxService.GetTaxRateAsync(null, taxCategoryId, customer);
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
            // TODO: (mg) (core) TaxService.GetTaxRateAsync usage is wrong. Get rate from GetPaymentMethodAdditionalFeeAsync.
            //var taxRate = await _taxService.GetTaxRateAsync(null, taxCategoryId, customer);
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
