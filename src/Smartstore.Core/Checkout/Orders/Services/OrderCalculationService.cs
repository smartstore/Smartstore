using System.Runtime.CompilerServices;
using Smartstore.Caching;
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
using Smartstore.Core.Common.Configuration;
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
        const string CartTaxingInfoKey = "CartTaxingInfos";

        private readonly SmartDbContext _db;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductService _productService;
        private readonly IDiscountService _discountService;
        private readonly IShippingService _shippingService;
        private readonly IGiftCardService _giftCardService;
        private readonly ICurrencyService _currencyService;
        private readonly IRoundingHelper _roundingHelper;
        private readonly IRequestCache _requestCache;
        private readonly IProviderManager _providerManager;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ITaxService _taxService;
        private readonly ITaxCalculator _taxCalculator;
        private readonly TaxSettings _taxSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly PriceSettings _priceSettings;
        private readonly CurrencySettings _currencySettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly Currency _primaryCurrency;
        private readonly Currency _workingCurrency;

        public OrderCalculationService(
            SmartDbContext db,
            IPriceCalculationService priceCalculationService,
            IProductService productService,
            IDiscountService discountService,
            IShippingService shippingService,
            IGiftCardService giftCardService,
            ICurrencyService currencyService,
            IRoundingHelper roundingHelper,
            IRequestCache requestCache,
            IProviderManager providerManager,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            IWorkContext workContext,
            IStoreContext storeContext,
            ITaxService taxService,
            ITaxCalculator taxCalculator,
            TaxSettings taxSettings,
            RewardPointsSettings rewardPointsSettings,
            PriceSettings priceSettings,
            CurrencySettings currencySettings,
            ShippingSettings shippingSettings)
        {
            _db = db;
            _priceCalculationService = priceCalculationService;
            _productService = productService;
            _discountService = discountService;
            _shippingService = shippingService;
            _giftCardService = giftCardService;
            _currencyService = currencyService;
            _roundingHelper = roundingHelper;
            _requestCache = requestCache;
            _providerManager = providerManager;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            _workContext = workContext;
            _storeContext = storeContext;
            _taxService = taxService;
            _taxCalculator = taxCalculator;
            _taxSettings = taxSettings;
            _rewardPointsSettings = rewardPointsSettings;
            _priceSettings = priceSettings;
            _currencySettings = currencySettings;
            _shippingSettings = shippingSettings;

            _primaryCurrency = currencyService.PrimaryCurrency;
            _workingCurrency = workContext.WorkingCurrency;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual async Task<ShoppingCartTotal> GetShoppingCartTotalAsync(
            ShoppingCart cart,
            bool includeRewardPoints = true,
            bool includePaymentFee = true,
            bool includeCreditBalance = true)
        {
            Guard.NotNull(cart);

            var cacheKey = $"ordercalculation:carttotal:{cart.GetHashCode()}-{includeRewardPoints}-{includePaymentFee}-{includeCreditBalance}";

            // INFO: CartTotalRule uses AsyncLock on this method! IRequestCache.Get would deadlock cart page.
            if (_requestCache.Contains(cacheKey))
            {
                return _requestCache.Get<ShoppingCartTotal>(cacheKey, null);
            }

            var store = _storeContext.CurrentStore;
            var customer = cart.Customer;
            var paymentMethodSystemName = customer != null
                ? customer.GenericAttributes.SelectedPaymentMethod
                : string.Empty;

            var subtotal = await GetCartSubtotalAsync(cart, false);
            var subtotalBase = subtotal.SubtotalWithDiscount;

            // Shipping without tax.
            var shipping = await GetCartShippingTotalAsync(cart, false);

            // Payment method additional fee without tax.
            var paymentFeeWithoutTax = 0m;
            if (includePaymentFee && paymentMethodSystemName.HasValue())
            {
                var paymentFee = await GetShoppingCartPaymentFeeAsync(cart, paymentMethodSystemName);
                if (paymentFee != 0m)
                {
                    var paymentFeeExclTax = await _taxCalculator.CalculatePaymentFeeTaxAsync(paymentFee.Amount, false, customer: customer);
                    paymentFeeWithoutTax = paymentFeeExclTax.Price;
                }
            }

            // Tax.
            var (shoppingCartTax, _) = await GetCartTaxTotalAsync(cart, includePaymentFee);

            // Cart total.
            var total = _roundingHelper.RoundIfEnabledFor(subtotalBase + (shipping.ShippingTotal ?? 0m) + paymentFeeWithoutTax + shoppingCartTax);

            // Order total discount.
            var (discountAmount, appliedDiscount) = await GetDiscountAmountAsync(total, DiscountType.AssignedToOrderTotal, customer);
            discountAmount = _roundingHelper.RoundIfEnabledFor(discountAmount);

            if (total < discountAmount)
            {
                discountAmount = total;
            }

            // Reduce total by discount amount.
            total = _roundingHelper.RoundIfEnabledFor(Math.Max(total - discountAmount, 0m));

            var amountsToVerify = new List<decimal>
            {
                subtotalBase,
                shipping.ShippingTotal ?? 0m,
                paymentFeeWithoutTax,
                shoppingCartTax,
                -discountAmount,
            };

            // Applied gift cards.
            var appliedGiftCards = new List<AppliedGiftCard>();
            if (!cart.IncludesMatchingItems(x => x.IsRecurring))
            {
                var giftCards = await _giftCardService.GetValidGiftCardsAsync(store.Id, customer);
                foreach (var gc in giftCards)
                {
                    if (total > 0m)
                    {
                        var usableAmount = total > gc.UsableAmount.Amount ? gc.UsableAmount.Amount : total;

                        // Reduce subtotal.
                        total -= usableAmount;
                        amountsToVerify.Add(-usableAmount);

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
            var redeemedRewardPointsAmount = 0m;

            if (_rewardPointsSettings.Enabled &&
                includeRewardPoints &&
                total > 0m &&
                customer != null &&
                customer.GenericAttributes.UseRewardPointsDuringCheckout)
            {
                var rewardPointsBalance = customer.GetRewardPointsBalance();
                var rewardPointsBalanceAmount = ConvertRewardPointsToAmountCore(rewardPointsBalance);

                if (total > rewardPointsBalanceAmount)
                {
                    redeemedRewardPointsAmount = rewardPointsBalanceAmount;
                    redeemedRewardPoints = rewardPointsBalance;
                }
                else
                {
                    redeemedRewardPointsAmount = total;
                    redeemedRewardPoints = ConvertAmountToRewardPoints(redeemedRewardPointsAmount);
                }
            }

            total = _roundingHelper.RoundIfEnabledFor(Math.Max(total, 0m));

            var totalConverted = total;
            var appliedCreditBalance = 0m;
            var toNearestRounding = 0m;
            var toNearestRoundingConverted = 0m;

            if (shipping.ShippingTotal.HasValue)
            {
                total -= redeemedRewardPointsAmount;
                amountsToVerify.Add(-redeemedRewardPointsAmount);

                // Credit balance.
                if (includeCreditBalance && customer != null && total > 0m)
                {
                    var creditBalance = customer.GenericAttributes.UseCreditBalanceDuringCheckout;
                    if (creditBalance > 0m)
                    {
                        if (creditBalance > total)
                        {
                            // Normalize used amount.
                            appliedCreditBalance = total;

                            customer.GenericAttributes.UseCreditBalanceDuringCheckout = total;
                            await _db.SaveChangesAsync();
                        }
                        else
                        {
                            appliedCreditBalance = creditBalance;
                        }
                    }
                }

                total = _roundingHelper.RoundIfEnabledFor(total - appliedCreditBalance);
                totalConverted = _currencyService.ConvertToWorkingCurrency(total).Amount;
                amountsToVerify.Add(-appliedCreditBalance);

                // Round order total to nearest (cash rounding).
                if (_workingCurrency.RoundOrderTotalEnabled 
                    && paymentMethodSystemName.HasValue()
                    && await _db.PaymentMethods.AnyAsync(x => x.PaymentMethodSystemName == paymentMethodSystemName && x.RoundOrderTotalEnabled))
                {
                    // The check for rounding differences is not necessary if the total is rounded anyway.
                    amountsToVerify.Clear();
                    total = _roundingHelper.ToNearest(total, out toNearestRounding);
                    totalConverted = _roundingHelper.ToNearest(totalConverted, out toNearestRoundingConverted);
                }

                total = CheckCartTotalRoundingDifference(total, false, amountsToVerify);
                totalConverted = CheckCartTotalRoundingDifference(totalConverted, true, amountsToVerify);
            }

            var shoppingCartTotal = new ShoppingCartTotal
            {
                Total = shipping.ShippingTotal.HasValue ? new(total, _primaryCurrency) : null,
                ToNearestRounding = new(toNearestRounding, _primaryCurrency),
                DiscountAmount = new(discountAmount, _primaryCurrency),
                AppliedDiscount = appliedDiscount,
                RedeemedRewardPoints = redeemedRewardPoints,
                RedeemedRewardPointsAmount = new(redeemedRewardPointsAmount, _primaryCurrency),
                CreditBalance = new(appliedCreditBalance, _primaryCurrency),
                AppliedGiftCards = appliedGiftCards,
                LineItems = subtotal.LineItems,
                ConvertedAmount = new()
                {
                    Total = shipping.ShippingTotal.HasValue ? new(totalConverted, _workingCurrency) : null,
                    ToNearestRounding = new(toNearestRoundingConverted, _workingCurrency)
                }
            };

            _requestCache.Put(cacheKey, shoppingCartTotal);

            return shoppingCartTotal;
        }

        public virtual async Task<ShoppingCartSubtotal> GetShoppingCartSubtotalAsync(ShoppingCart cart, bool? includeTax = null, ProductBatchContext batchContext = null)
        {
            includeTax ??= _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;

            var cacheKey = $"ordercalculation:cartsubtotal:{cart.GetHashCode()}-{includeTax}";

            // INFO: CartSubtotalRule uses AsyncLock on this method! IRequestCache.Get would deadlock cart page.
            if (_requestCache.Contains(cacheKey))
            {
                return _requestCache.Get<ShoppingCartSubtotal>(cacheKey, null);
            }

            var subtotal = await GetCartSubtotalAsync(cart, includeTax.Value, batchContext);
            var result = new ShoppingCartSubtotal
            {
                SubtotalWithoutDiscount = new(subtotal.SubtotalWithoutDiscount, _primaryCurrency),
                SubtotalWithDiscount = new(subtotal.SubtotalWithDiscount, _primaryCurrency),
                DiscountAmount = new(subtotal.DiscountAmount, _primaryCurrency),
                AppliedDiscount = subtotal.AppliedDiscount,
                TaxRates = subtotal.TaxRates,
                LineItems = subtotal.LineItems
            };

            _requestCache.Put(cacheKey, result);

            return result;
        }

        public virtual async Task<ShoppingCartShippingTotal> GetShoppingCartShippingTotalAsync(ShoppingCart cart, bool? includeTax = null)
        {
            includeTax ??= _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;

            var result = await GetCartShippingTotalAsync(cart, includeTax.Value);

            return new ShoppingCartShippingTotal
            {
                ShippingTotal = result.ShippingTotal.HasValue ? new(result.ShippingTotal.Value, _primaryCurrency) : null,
                AppliedDiscount = result.AppliedDiscount,
                TaxRate = result.TaxRate
            };
        }

        public virtual async Task<(Money Price, TaxRatesDictionary TaxRates)> GetShoppingCartTaxTotalAsync(ShoppingCart cart, bool includePaymentFee = true)
        {
            var (amount, taxRates) = await GetCartTaxTotalAsync(cart, includePaymentFee);

            return (new(amount, _primaryCurrency), taxRates);
        }

        public virtual async Task<bool> IsFreeShippingAsync(ShoppingCart cart)
        {
            Guard.NotNull(cart);

            if (cart.Customer != null)
            {
                // Check whether customer is in a customer role with free shipping applied.
                await _db.LoadCollectionAsync(cart.Customer, x => x.CustomerRoleMappings, false, x => x.Include(y => y.CustomerRole));

                var customerRoles = cart.Customer.CustomerRoleMappings
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
            if (!cart.Items.Any(x => x.Item.IsShippingEnabled && !x.Item.IsFreeShipping))
            {
                return true;
            }

            // Check if the subtotal is large enough for free shipping.
            if (_shippingSettings.FreeShippingOverXEnabled)
            {
                var subtotal = await GetCartSubtotalAsync(cart, _shippingSettings.FreeShippingOverXIncludingTax);

                if (subtotal.SubtotalWithDiscount > _shippingSettings.FreeShippingOverXValue)
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<Money> GetShoppingCartShippingChargeAsync(ShoppingCart cart)
            => new(await GetShippingChargeAsync(cart), _primaryCurrency);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<Money> GetShoppingCartPaymentFeeAsync(ShoppingCart cart, string paymentMethodSystemName)
            => new(await GetCartPaymentFeeAsync(cart, paymentMethodSystemName), _primaryCurrency);

        public virtual async Task<(decimal Amount, Discount AppliedDiscount)> AdjustShippingRateAsync(
            ShoppingCart cart,
            decimal shippingRate,
            ShippingOption shippingOption,
            IList<ShippingMethod> shippingMethods)
        {
            Guard.NotNull(cart);

            if (await IsFreeShippingAsync(cart))
            {
                return (0m, null);
            }

            var ignoreAdditionalShippingCharge = false;
            var bundlePerItemShipping = 0m;
            var adjustedRate = 0m;

            foreach (var cartItem in cart.Items)
            {
                var item = cartItem.Item;

                if (item.Product != null && item.Product.ProductType == ProductType.BundledProduct && item.Product.BundlePerItemShipping)
                {
                    foreach (var childItem in cartItem.ChildItems.Where(x => x.Item.IsShippingEnabled && !x.Item.IsFreeShipping))
                    {
                        bundlePerItemShipping += shippingRate;
                    }
                }
                else if (adjustedRate == 0m)
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
            var (discountAmount, discount) = await GetDiscountAmountAsync(adjustedRate, DiscountType.AssignedToShipping, cart.Customer);
            adjustedRate = _roundingHelper.RoundIfEnabledFor(Math.Max(adjustedRate - discountAmount, 0m));

            return (adjustedRate, discount);
        }

        public virtual async Task<(Money Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(Money amount, DiscountType discountType, Customer customer)
        {
            var (discountAmount, appliedDiscount) = await GetDiscountAmountAsync(amount.Amount, discountType, customer);
            discountAmount = _roundingHelper.RoundIfEnabledFor(discountAmount);

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
            => (CartTaxingInfo)cartItem.CustomProperties[CartTaxingInfoKey];

        private int GetTaxCategoryId(ShoppingCart cart, int defaultTaxCategoryId)
        {
            if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.HighestCartAmount)
            {
                return cart.Items.FirstOrDefault(x => GetTaxingInfo(x).HasHighestCartAmount)?.Item?.Product?.TaxCategoryId ?? defaultTaxCategoryId;
            }
            else if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.HighestTaxRate)
            {
                return cart.Items.FirstOrDefault(x => GetTaxingInfo(x).HasHighestTaxRate)?.Item?.Product?.TaxCategoryId ?? defaultTaxCategoryId;
            }

            return defaultTaxCategoryId;
        }

        protected virtual async Task<CartSubtotal> GetCartSubtotalAsync(ShoppingCart cart, bool includeTax, ProductBatchContext batchContext = null)
        {
            Guard.NotNull(cart);

            var result = new CartSubtotal();

            if (!cart.HasItems)
            {
                return result;
            }

            var customer = cart.Customer;
            var subtotalExclTaxWithoutDiscount = 0m;
            var subtotalInclTaxWithoutDiscount = 0m;

            batchContext ??= _productService.CreateProductBatchContext(cart.Items.Select(x => x.Item.Product).ToArray(), null, customer, false);

            var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, customer, _primaryCurrency, batchContext);

            foreach (var cartItem in cart.Items)
            {
                if (cartItem.Item.Product == null)
                {
                    continue;
                }

                var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(cartItem, calculationOptions);
                var (unitPrice, subtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);

                //"- net:{0,10:N6} gross:{1,10:N6} final:{2,10:N6} excludingTax:{3}".FormatInvariant(
                //    subtotal.Tax.Value.PriceNet,
                //    subtotal.Tax.Value.PriceGross,
                //    subtotal.FinalPrice.Amount,
                //    !subtotal.Tax.Value.Inclusive).Dump();

                var tax = subtotal.Tax.Value;
                var itemExclTax = _roundingHelper.RoundIfEnabledFor(tax.PriceNet);
                var itemInclTax = _roundingHelper.RoundIfEnabledFor(tax.PriceGross);

                subtotalExclTaxWithoutDiscount += itemExclTax;
                subtotalInclTaxWithoutDiscount += itemInclTax;

                result.TaxRates.Add(tax.Rate.Rate, itemInclTax - itemExclTax);

                result.LineItems.Add(new ShoppingCartLineItem(cartItem)
                {
                    UnitPrice = unitPrice,
                    Subtotal = subtotal
                });
            }

            // Checkout attributes.
            if (customer != null)
            {
                var values = await _checkoutAttributeMaterializer.MaterializeCheckoutAttributeValuesAsync(customer.GenericAttributes.CheckoutAttributes);
                if (values != null)
                {
                    foreach (var value in values)
                    {
                        var attributeTax = await _taxCalculator.CalculateCheckoutAttributeTaxAsync(value, customer: customer);

                        subtotalExclTaxWithoutDiscount += attributeTax.PriceNet;
                        subtotalInclTaxWithoutDiscount += attributeTax.PriceGross;

                        result.TaxRates.Add(attributeTax.Rate.Rate, attributeTax.Amount);
                    }
                }
            }

            // Subtotal without discount.
            result.SubtotalWithoutDiscount = _roundingHelper.RoundIfEnabledFor(Math.Max(includeTax ? subtotalInclTaxWithoutDiscount : subtotalExclTaxWithoutDiscount, 0m));

            // We calculate discount amount on order subtotal excl tax (discount first).
            var (discountAmountExclTax, appliedDiscount) = await GetDiscountAmountAsync(subtotalExclTaxWithoutDiscount, DiscountType.AssignedToOrderSubTotal, customer);

            if (subtotalExclTaxWithoutDiscount < discountAmountExclTax)
            {
                discountAmountExclTax = subtotalExclTaxWithoutDiscount;
            }

            var discountAmountInclTax = discountAmountExclTax;

            // Subtotal with discount (excl tax).
            var subtotalExclTaxWithDiscount = subtotalExclTaxWithoutDiscount - discountAmountExclTax;
            var subtotalInclTaxWithDiscount = subtotalExclTaxWithDiscount;

            // Add tax for shopping items & checkout attributes.
            var tempTaxRates = new Dictionary<decimal, decimal>(result.TaxRates);
            foreach (var kvp in tempTaxRates)
            {
                var taxRate = kvp.Key;
                var taxAmount = kvp.Value;

                if (taxAmount != 0m)
                {
                    // Discount the tax amount that applies to subtotal items.
                    if (subtotalExclTaxWithoutDiscount > 0m)
                    {
                        var discountTax = result.TaxRates[taxRate] * (discountAmountExclTax / subtotalExclTaxWithoutDiscount);
                        discountAmountInclTax += discountTax;
                        taxAmount = _roundingHelper.RoundIfEnabledFor(result.TaxRates[taxRate] - discountTax);
                        result.TaxRates[taxRate] = taxAmount;
                    }

                    // Subtotal with discount (incl tax).
                    subtotalInclTaxWithDiscount += taxAmount;
                }
            }

            discountAmountExclTax = _roundingHelper.RoundIfEnabledFor(discountAmountExclTax);
            discountAmountInclTax = _roundingHelper.RoundIfEnabledFor(discountAmountInclTax);

            result.SubtotalWithDiscount = _roundingHelper.RoundIfEnabledFor(Math.Max(includeTax ? subtotalInclTaxWithDiscount : subtotalExclTaxWithDiscount, 0m));
            result.DiscountAmount = includeTax ? discountAmountInclTax : discountAmountExclTax;
            result.AppliedDiscount = appliedDiscount;

            return result;
        }

        protected virtual async Task<CartShippingTotal> GetCartShippingTotalAsync(ShoppingCart cart, bool includeTax)
        {
            Guard.NotNull(cart);

            if (await IsFreeShippingAsync(cart))
            {
                return new CartShippingTotal { ShippingTotal = 0 };
            }

            var (shippingTotalAmount, appliedDiscount) = await GetAdjustedShippingTotalAsync(cart);
            if (!shippingTotalAmount.HasValue)
            {
                return new CartShippingTotal();
            }

            var shippingTotal = _roundingHelper.RoundIfEnabledFor(Math.Max(shippingTotalAmount.Value, 0m));

            await PrepareAuxiliaryServicesTaxingInfosAsync(cart);

            // Commented out because requires several plugins to be updated and migration of Order.OrderShippingTaxRate and Order.PaymentMethodAdditionalFeeTaxRate.
            //if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.ProRata)
            //{
            //	// calculate proRataShipping: get product weightings for cart and multiply them with the shipping amount
            //	shippingTotalTaxed = decimal.Zero;

            //	var tmpTaxRate = decimal.Zero;
            //	var taxRates = new List<decimal>();

            //	foreach (var item in cart.Items)
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
            var tax = await _taxCalculator.CalculateShippingTaxAsync(shippingTotal, includeTax, taxCategoryId, cart.Customer);

            return new CartShippingTotal
            {
                ShippingTotal = tax.Price,
                AppliedDiscount = appliedDiscount,
                TaxRate = tax.Rate.Rate
            };
        }

        protected virtual async Task<(decimal Amount, TaxRatesDictionary TaxRates)> GetCartTaxTotalAsync(ShoppingCart cart, bool includePaymentFee)
        {
            Guard.NotNull(cart);

            var customer = cart.Customer;
            var taxRates = new TaxRatesDictionary();
            var taxTotal = 0m;
            var subtotalTax = 0m;
            var shippingTax = 0m;
            var paymentFeeTax = 0m;

            //// (VATFIX)
            if (await _taxService.IsVatExemptAsync(customer, null))
            {
                taxRates.Add(0m, 0m);
                return (taxTotal, taxRates);
            }
            //// (VATFIX)

            // Order subtotal (cart items + checkout attributes).
            var subtotal = await GetCartSubtotalAsync(cart, false);

            foreach (var pair in subtotal.TaxRates)
            {
                subtotalTax += pair.Value;
                taxRates.Add(pair.Key, pair.Value);
            }

            // Shipping tax amount.
            if (_taxSettings.ShippingIsTaxable && !await IsFreeShippingAsync(cart))
            {
                var (shippingTotalAmount, _) = await GetAdjustedShippingTotalAsync(cart);
                if (shippingTotalAmount.HasValue)
                {
                    var shippingTotal = _roundingHelper.RoundIfEnabledFor(Math.Max(shippingTotalAmount.Value, 0m));

                    await PrepareAuxiliaryServicesTaxingInfosAsync(cart);

                    // Commented out because requires several plugins to be updated and migration of Order.OrderShippingTaxRate and Order.PaymentMethodAdditionalFeeTaxRate.
                    //if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.ProRata)
                    //{
                    //	// Calculate proRataShipping: get product weightings for cart and multiply them with the shipping amount.
                    //	foreach (var item in cart.Items)
                    //	{
                    //		var proRataShipping = shippingTotal.Value * GetTaxingInfo(item).ProRataWeighting;
                    //		shippingTax += GetShippingTaxAmount(proRataShipping, customer, item.Item.Product.TaxCategoryId, taxRates);
                    //	}
                    //}
                    //else
                    //{

                    var taxCategoryId = GetTaxCategoryId(cart, _taxSettings.ShippingTaxClassId);
                    var tax = await _taxCalculator.CalculateShippingTaxAsync(shippingTotal, null, taxCategoryId, customer);
                    var taxRate = tax.Rate.Rate;

                    shippingTax = _roundingHelper.RoundIfEnabledFor(Math.Max(tax.Amount, 0m));
                    taxRates.Add(taxRate, shippingTax);
                }
            }

            // Payment fee tax amount.
            if (includePaymentFee && _taxSettings.PaymentMethodAdditionalFeeIsTaxable && customer != null)
            {
                var paymentFee = await GetShoppingCartPaymentFeeAsync(cart, customer.GenericAttributes.SelectedPaymentMethod);
                if (paymentFee != 0m)
                {
                    await PrepareAuxiliaryServicesTaxingInfosAsync(cart);

                    // Commented out because requires several plugins to be updated and migration of Order.OrderShippingTaxRate and Order.PaymentMethodAdditionalFeeTaxRate.
                    //if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.ProRata)
                    //{
                    //	// Calculate proRataShipping: get product weightings for cart and multiply them with the shipping amount.
                    //	foreach (var item in cart.Items)
                    //	{
                    //		var proRataPaymentFees = paymentFee * GetTaxingInfo(item).ProRataWeighting;
                    //		paymentFeeTax += GetPaymentFeeTaxAmount(proRataPaymentFees, customer, item.Item.Product.TaxCategoryId, taxRates);
                    //	}
                    //}
                    //else
                    //{

                    var taxCategoryId = GetTaxCategoryId(cart, _taxSettings.PaymentMethodAdditionalFeeTaxClassId);
                    var tax = await _taxCalculator.CalculatePaymentFeeTaxAsync(paymentFee.Amount, null, taxCategoryId, customer);
                    var taxRate = tax.Rate.Rate;

                    paymentFeeTax = _roundingHelper.RoundIfEnabledFor(tax.Amount);

                    // In case of a payment fee the tax amount can be less zero!
                    // That's why we do not use helper TaxRatesDictionary.Add here.
                    if (taxRate > 0m && paymentFeeTax != 0m)
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
            if (taxRates.Count == 0)
            {
                taxRates.Add(0m, 0m);
            }

            taxTotal = _roundingHelper.RoundIfEnabledFor(Math.Max(subtotalTax + shippingTax + paymentFeeTax, 0m));

            return (taxTotal, taxRates);
        }

        protected virtual async Task PrepareAuxiliaryServicesTaxingInfosAsync(ShoppingCart cart)
        {
            // No additional infos required.
            if (!cart.HasItems || _taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.SpecifiedTaxCategory)
            {
                return;
            }

            // Additional infos already collected.
            if (cart.Items.First().CustomProperties.ContainsKey(CartTaxingInfoKey))
            {
                return;
            }

            // Instance taxing info objects.
            cart.Items.Each(x => x.CustomProperties[CartTaxingInfoKey] = new CartTaxingInfo());

            // Collect infos.
            if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.HighestCartAmount)
            {
                // Calculate all subtotals.
                var cartProducts = cart.Items.Select(x => x.Item.Product).ToArray();
                var batchContext = _productService.CreateProductBatchContext(cartProducts, null, cart.Customer, false);
                var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, cart.Customer, _primaryCurrency, batchContext);
                calculationOptions.IgnoreDiscounts = true;

                foreach (var item in cart.Items)
                {
                    var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(item, calculationOptions);
                    var (_, subtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);
                    GetTaxingInfo(item).SubtotalWithoutDiscount = subtotal.FinalPrice;
                }

                // Items with the highest subtotal.
                var highestAmountItems = cart.Items
                    .GroupBy(x => x.Item.Product.TaxCategoryId)
                    .OrderByDescending(x => x.Sum(y => GetTaxingInfo(y).SubtotalWithoutDiscount.Amount))
                    .First();

                // Mark items.
                highestAmountItems.Each(x => GetTaxingInfo(x).HasHighestCartAmount = true);
            }
            else if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.HighestTaxRate)
            {
                var maxTaxRate = 0m;
                var maxTaxCategoryId = 0;

                // Get tax category id with the highest rate.
                foreach (var item in cart.Items)
                {
                    var product = item.Item.Product;
                    var taxRate = await _taxService.GetTaxRateAsync(product, product.TaxCategoryId, cart.Customer);
                    if (taxRate > maxTaxRate)
                    {
                        maxTaxRate = taxRate;
                        maxTaxCategoryId = taxRate.TaxCategoryId;
                    }
                }

                // Mark items.
                cart.Items.Where(x => x.Item.Product.TaxCategoryId == maxTaxCategoryId)
                    .Each(x => GetTaxingInfo(x).HasHighestTaxRate = true);
            }
            //else if (_taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.ProRata)
            //{
            //	// calculate all subtotals
            //	cart.Items.Each(x => GetTaxingInfo(x).SubTotalWithoutDiscount = _priceCalculationService.GetSubTotal(x, false));

            //	// sum over all subtotals
            //	var subtotalSum = cart.Items.Sum(x => GetTaxingInfo(x).SubTotalWithoutDiscount);

            //	// calculate pro rata weightings
            //	cart.Each(x =>
            //	{
            //		var taxingInfo = GetTaxingInfo(x);
            //		taxingInfo.ProRataWeighting = taxingInfo.SubTotalWithoutDiscount / subtotalSum;
            //	});
            //}
        }

        protected virtual async Task<(decimal Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(decimal amount, DiscountType discountType, Customer customer)
        {
            var result = 0m;
            Discount appliedDiscount = null;

            if (!_priceSettings.IgnoreDiscounts)
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

            if (result < 0m)
            {
                result = 0m;
            }

            return (result, appliedDiscount);
        }

        protected virtual async Task<decimal> GetShippingChargeAsync(ShoppingCart cart)
        {
            var charge = 0m;

            if (!await IsFreeShippingAsync(cart))
            {
                foreach (var cartItem in cart.Items)
                {
                    var item = cartItem.Item;

                    if (item.IsShippingEnabled && !item.IsFreeShipping && item.Product != null)
                    {
                        if (_shippingSettings.ChargeOnlyHighestProductShippingSurcharge)
                        {
                            if (charge < item.Product.AdditionalShippingCharge)
                            {
                                charge = item.Product.AdditionalShippingCharge;
                            }
                        }
                        else
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

        protected virtual async Task<(decimal? Amount, Discount AppliedDiscount)> GetAdjustedShippingTotalAsync(ShoppingCart cart)
        {
            var shippingOption = cart.Customer?.GenericAttributes?.SelectedShippingOption?.Convert<ShippingOption>();

            if (shippingOption != null)
            {
                // Use last shipping option (get from cache).
                var shippingMethods = await _shippingService.GetAllShippingMethodsAsync(cart.StoreId);

                return await AdjustShippingRateAsync(cart, shippingOption.Rate, shippingOption, shippingMethods);
            }
            else
            {
                await _db.LoadReferenceAsync(cart.Customer, x => x.ShippingAddress);

                // Use fixed rate (if possible).
                var shippingAddress = cart.Customer?.ShippingAddress ?? null;
                var shippingRateComputationMethods = _shippingService.LoadEnabledShippingProviders(cart.StoreId);

                if (!shippingRateComputationMethods.Any())
                {
                    throw new InvalidOperationException(T("Shipping.CouldNotLoadMethod"));
                }

                if (shippingRateComputationMethods.Count() == 1)
                {
                    var shippingRateComputationMethod = shippingRateComputationMethods.First();
                    var getShippingOptionRequest = _shippingService.CreateShippingOptionRequest(cart, shippingAddress, cart.StoreId);
                    var fixedRate = await shippingRateComputationMethod.Value.GetFixedRateAsync(getShippingOptionRequest);

                    if (fixedRate.HasValue)
                    {
                        // Ignore returned currency. The caller specifies it to avoid mixed currencies during calculation.
                        return await AdjustShippingRateAsync(cart, fixedRate.Value, null, null);
                    }
                }
            }

            return (null, null);
        }

        protected virtual async Task<decimal> GetCartPaymentFeeAsync(ShoppingCart cart, string paymentMethodSystemName)
        {
            Guard.NotNull(cart);

            var paymentFee = 0m;
            var provider = _providerManager.GetProvider<IPaymentMethod>(paymentMethodSystemName);
            if (provider != null)
            {
                var (fixedFeeOrPercentage, usePercentage) = await provider.Value.GetPaymentFeeInfoAsync(cart);
                if (fixedFeeOrPercentage != 0m)
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

            return _roundingHelper.RoundIfEnabledFor(paymentFee);
        }

        /// <summary>
        /// Checks the total amount for rounding differences compared to the sum of the rounded sub amounts (subtotal, shipping costs, tax, total discount etc.).
        /// A difference (typically 1 cent) may occur if rounding is not used during shopping cart calculation, but prices are usually always displayed rounded.
        /// </summary>
        protected virtual decimal CheckCartTotalRoundingDifference(decimal cartTotal, bool convert, List<decimal> amountsToVerify)
        {
            if (cartTotal != 0m 
                && amountsToVerify.Count > 0 
                && _currencySettings.RoundOrderTotalDifference > 0
                && !_roundingHelper.IsShoppingCartRoundingEnabled())
            {
                var roundedTotal = _roundingHelper.Round(cartTotal);
                var verifiedTotal = amountsToVerify.Sum(x => _roundingHelper.Round(convert ? _currencyService.ConvertToWorkingCurrency(x).Amount : x));
                var difference = _roundingHelper.ToSmallestCurrencyUnit(decimal.Abs(roundedTotal - verifiedTotal), _workingCurrency);

                "- total{0}:{1} check:{2} difference:{3}".FormatInvariant(convert ? " converted" : string.Empty, roundedTotal, verifiedTotal, difference).Dump();

                // Check for rounding difference in the smallest currency unit.
                // Be careful not to obscure other calculation errors here.
                if (difference <= _currencySettings.RoundOrderTotalDifference)
                {
                    return verifiedTotal;
                }
            }

            return cartTotal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual decimal ConvertRewardPointsToAmountCore(int rewardPoints)
            => _roundingHelper.RoundIfEnabledFor(rewardPoints > 0 ? rewardPoints * _rewardPointsSettings.ExchangeRate : 0m);

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

        protected class CartSubtotal
        {
            public decimal SubtotalWithoutDiscount { get; set; }
            public decimal SubtotalWithDiscount { get; set; }
            public decimal DiscountAmount { get; set; }
            public Discount AppliedDiscount { get; set; }
            public TaxRatesDictionary TaxRates { get; set; } = new();
            public List<ShoppingCartLineItem> LineItems { get; set; } = new();
        }

        protected class CartShippingTotal
        {
            public decimal? ShippingTotal { get; set; }
            public Discount AppliedDiscount { get; set; }
            public decimal TaxRate { get; set; }
        }

        protected class CartTaxingInfo
        {
            public Money SubtotalWithoutDiscount { get; internal set; }
            public bool HasHighestCartAmount { get; internal set; }
            public bool HasHighestTaxRate { get; internal set; }
            public decimal ProRataWeighting { get; internal set; } = 0m;
        }
    }
}
