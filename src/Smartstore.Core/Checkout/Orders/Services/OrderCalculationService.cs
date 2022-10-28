using System.Runtime.CompilerServices;
using Smartstore.Core.Catalog;
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
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductService _productService;
        private readonly IDiscountService _discountService;
        private readonly IShippingService _shippingService;
        private readonly IGiftCardService _giftCardService;
        private readonly ICurrencyService _currencyService;
        private readonly IProviderManager _providerManager;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ITaxService _taxService;
        private readonly ITaxCalculator _taxCalculator;
        private readonly TaxSettings _taxSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly PriceSettings _priceSettings;
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
            IProviderManager providerManager,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            IWorkContext workContext,
            IStoreContext storeContext,
            ITaxService taxService,
            ITaxCalculator taxCalculator,
            TaxSettings taxSettings,
            RewardPointsSettings rewardPointsSettings,
            CatalogSettings catalogSettings,
            PriceSettings priceSettings,
            ShippingSettings shippingSettings)
        {
            _db = db;
            _priceCalculationService = priceCalculationService;
            _productService = productService;
            _discountService = discountService;
            _shippingService = shippingService;
            _giftCardService = giftCardService;
            _currencyService = currencyService;
            _providerManager = providerManager;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            _workContext = workContext;
            _storeContext = storeContext;
            _taxService = taxService;
            _taxCalculator = taxCalculator;
            _taxSettings = taxSettings;
            _rewardPointsSettings = rewardPointsSettings;
            _catalogSettings = catalogSettings;
            _priceSettings = priceSettings;
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
            Guard.NotNull(cart, nameof(cart));

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
            var paymentFeeWithoutTax = decimal.Zero;
            if (includePaymentFee && paymentMethodSystemName.HasValue())
            {
                var paymentFee = await GetShoppingCartPaymentFeeAsync(cart, paymentMethodSystemName);
                if (paymentFee != decimal.Zero)
                {
                    var paymentFeeExclTax = await _taxCalculator.CalculatePaymentFeeTaxAsync(paymentFee.Amount, false, customer: customer);
                    paymentFeeWithoutTax = paymentFeeExclTax.Price;
                }
            }

            // Tax.
            var (shoppingCartTax, _) = await GetCartTaxTotalAsync(cart, includePaymentFee);

            // Order total.
            var resultTemp = subtotalBase;

            if (shipping.ShippingTotal.HasValue)
            {
                resultTemp += shipping.ShippingTotal.Value;
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
            decimal? orderTotal = shipping.ShippingTotal.HasValue ? resultTemp : null;
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
                LineItems = subtotal.LineItems,
                ConvertedAmount = new ShoppingCartTotal.ConvertedAmounts
                {
                    Total = orderTotalConverted.HasValue ? new(orderTotalConverted.Value, _workingCurrency) : null,
                    ToNearestRounding = new(toNearestRoundingConverted, _workingCurrency)
                }
            };

            return result;
        }

        public virtual async Task<ShoppingCartSubtotal> GetShoppingCartSubtotalAsync(ShoppingCart cart, bool? includeTax = null, ProductBatchContext batchContext = null)
        {
            includeTax ??= _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;

            var result = await GetCartSubtotalAsync(cart, includeTax.Value, batchContext);

            return new ShoppingCartSubtotal
            {
                SubtotalWithoutDiscount = new(result.SubtotalWithoutDiscount, _primaryCurrency),
                SubtotalWithDiscount = new(result.SubtotalWithDiscount, _primaryCurrency),
                DiscountAmount = new(result.DiscountAmount, _primaryCurrency),
                AppliedDiscount = result.AppliedDiscount,
                TaxRates = result.TaxRates,
                LineItems = result.LineItems
            };
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
            Guard.NotNull(cart, nameof(cart));

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
            Guard.NotNull(cart, nameof(cart));

            if (await IsFreeShippingAsync(cart))
            {
                return (decimal.Zero, null);
            }

            var ignoreAdditionalShippingCharge = false;
            var bundlePerItemShipping = decimal.Zero;
            var adjustedRate = decimal.Zero;

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
            var (discountAmount, discount) = await GetDiscountAmountAsync(adjustedRate, DiscountType.AssignedToShipping, cart.Customer);
            adjustedRate = _workingCurrency.RoundIfEnabledFor(Math.Max(adjustedRate - discountAmount, decimal.Zero));

            return (adjustedRate, discount);
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
            Guard.NotNull(cart, nameof(cart));

            var result = new CartSubtotal();

            if (!cart.Items.Any())
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

                // There may occur rounding differences between the subtotal and the sum of the line subtotals if RoundOrderItemsEnabled is 'false'.
                var tax = subtotal.Tax.Value;
                var itemExclTax = _workingCurrency.RoundIfEnabledFor(tax.PriceNet);
                var itemInclTax = _workingCurrency.RoundIfEnabledFor(tax.PriceGross);

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

                        result.TaxRates.Add(attributeTax.Amount, attributeTax.Rate.Rate);
                    }
                }
            }

            // Subtotal without discount.
            result.SubtotalWithoutDiscount = _workingCurrency.RoundIfEnabledFor(Math.Max(includeTax ? subtotalInclTaxWithoutDiscount : subtotalExclTaxWithoutDiscount, 0m));

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

                if (taxAmount != decimal.Zero)
                {
                    // Discount the tax amount that applies to subtotal items.
                    if (subtotalExclTaxWithoutDiscount > decimal.Zero)
                    {
                        var discountTax = result.TaxRates[taxRate] * (discountAmountExclTax / subtotalExclTaxWithoutDiscount);
                        discountAmountInclTax += discountTax;
                        taxAmount = _workingCurrency.RoundIfEnabledFor(result.TaxRates[taxRate] - discountTax);
                        result.TaxRates[taxRate] = taxAmount;
                    }

                    // Subtotal with discount (incl tax).
                    subtotalInclTaxWithDiscount += taxAmount;
                }
            }

            // Why no rounding of discountAmountExclTax here?
            discountAmountInclTax = _workingCurrency.RoundIfEnabledFor(discountAmountInclTax);

            result.SubtotalWithDiscount = _workingCurrency.RoundIfEnabledFor(Math.Max(includeTax ? subtotalInclTaxWithDiscount : subtotalExclTaxWithDiscount, 0m));
            result.DiscountAmount = includeTax ? discountAmountInclTax : discountAmountExclTax;
            result.AppliedDiscount = appliedDiscount;

            return result;
        }

        protected virtual async Task<CartShippingTotal> GetCartShippingTotalAsync(ShoppingCart cart, bool includeTax)
        {
            Guard.NotNull(cart, nameof(cart));

            if (await IsFreeShippingAsync(cart))
            {
                return new CartShippingTotal { ShippingTotal = 0 };
            }

            var (shippingTotalAmount, appliedDiscount) = await GetAdjustedShippingTotalAsync(cart);
            if (!shippingTotalAmount.HasValue)
            {
                return new CartShippingTotal();
            }

            var shippingTotal = _workingCurrency.RoundIfEnabledFor(Math.Max(shippingTotalAmount.Value, 0m));

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
            Guard.NotNull(cart, nameof(cart));

            var customer = cart.Customer;
            var taxRates = new TaxRatesDictionary();
            var taxTotal = decimal.Zero;
            var subtotalTax = decimal.Zero;
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
                    var shippingTotal = _workingCurrency.RoundIfEnabledFor(Math.Max(shippingTotalAmount.Value, 0m));

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

                    shippingTax = _workingCurrency.RoundIfEnabledFor(Math.Max(tax.Amount, 0m));
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

                    paymentFeeTax = _workingCurrency.RoundIfEnabledFor(tax.Amount);

                    // In case of a payment fee the tax amount can be less zero!
                    // That's why we do not use helper TaxRatesDictionary.Add here.
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

            taxTotal = _workingCurrency.RoundIfEnabledFor(Math.Max(subtotalTax + shippingTax + paymentFeeTax, decimal.Zero));

            return (taxTotal, taxRates);
        }

        protected virtual async Task PrepareAuxiliaryServicesTaxingInfosAsync(ShoppingCart cart)
        {
            // No additional infos required.
            if (!cart.Items.Any() || _taxSettings.AuxiliaryServicesTaxingType == AuxiliaryServicesTaxType.SpecifiedTaxCategory)
            {
                return;
            }

            // Additional infos already collected.
            if (cart.Items.First().CustomProperties.ContainsKey(CART_TAXING_INFO_KEY))
            {
                return;
            }

            // Instance taxing info objects.
            cart.Items.Each(x => x.CustomProperties[CART_TAXING_INFO_KEY] = new CartTaxingInfo());

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
                var maxTaxRate = decimal.Zero;
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
            var result = decimal.Zero;
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

        protected virtual async Task<decimal> GetShippingChargeAsync(ShoppingCart cart)
        {
            var charge = decimal.Zero;

            if (!await IsFreeShippingAsync(cart))
            {
                foreach (var cartItem in cart.Items)
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
                var shippingRateComputationMethods = _shippingService.LoadActiveShippingRateComputationMethods(cart.StoreId);

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
