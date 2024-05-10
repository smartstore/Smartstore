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
            _shippingSettings = shippingSettings;

            _primaryCurrency = currencyService.PrimaryCurrency;
            _workingCurrency = workContext.WorkingCurrency;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual async Task<ShoppingCartTotal> GetShoppingCartTotalAsync(
            ShoppingCart cart,
            bool includeRewardPoints = true,
            bool includePaymentFee = true,
            bool includeCreditBalance = true,
            ProductBatchContext batchContext = null,
            bool cache = true)
        {
            Guard.NotNull(cart);

            var cacheKey = $"ordercalculation:carttotal:{cart.GetHashCode()}-{includeRewardPoints}-{includePaymentFee}-{includeCreditBalance}";

            // INFO: CartTotalRule uses AsyncLock on this method! IRequestCache.Get would deadlock cart page.
            if (cache && _requestCache.Contains(cacheKey))
            {
                return _requestCache.Get<ShoppingCartTotal>(cacheKey, null);
            }

            var customer = cart.Customer;
            var includeTax = _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
            var paymentMethodSystemName = customer != null ? customer.GenericAttributes.SelectedPaymentMethod : string.Empty;

            var (cartTaxTotal, _) = await GetCartTaxTotalAsync(cart, includePaymentFee);
            var cartTax = Round(includeTax ? 0m : cartTaxTotal);

            var subtotal = await GetCartSubtotalAsync(cart, false, batchContext);
            var subtotalWithDiscount = Round(includeTax ? subtotal.SubtotalWithDiscountGross : subtotal.SubtotalWithDiscountNet);
            var subtotalWithoutDiscount = Round(includeTax ? subtotal.SubtotalWithoutDiscountGross : subtotal.SubtotalWithoutDiscountNet);
            var subtotalDiscount = Round(includeTax ? subtotal.DiscountAmountGross : subtotal.DiscountAmountNet);

            var cartShipping = await GetCartShippingTotalAsync(cart, false);
            var shipping = cartShipping != null ? Round(includeTax ? cartShipping.Tax.PriceGross : cartShipping.Tax.PriceNet) : Round(0m);

            var paymentFee = Round(0m);
            if (includePaymentFee && paymentMethodSystemName.HasValue())
            {
                var fee = await GetShoppingCartPaymentFeeAsync(cart, paymentMethodSystemName);
                if (fee.Amount != 0m)
                {
                    var tax = await _taxCalculator.CalculatePaymentFeeTaxAsync(fee.Amount, false, null, customer);
                    paymentFee = Round(includeTax ? tax.PriceGross : tax.PriceNet);
                }
            }

            // Order total discount.
            var tempTotal = _roundingHelper.Round(subtotalWithDiscount.Amount + cartTax.Amount + shipping.Amount + paymentFee.Amount);
            var (totalDiscount, appliedDiscount) = await GetDiscountAmountAsync(tempTotal, DiscountType.AssignedToOrderTotal, customer);
            //$"- temp total {tempTotal}".Dump();
            totalDiscount = _roundingHelper.Round(totalDiscount);
            totalDiscount = tempTotal < totalDiscount ? tempTotal : totalDiscount;

            // INFO: see OrderTotalsViewComponent. Converts ShoppingCartTotal.DiscountAmount aka totalDiscount.
            var totalDiscountConverted = Round(totalDiscount).Converted;

            var total = _roundingHelper.Round(
                subtotalWithoutDiscount.Amount
                - subtotalDiscount.Amount
                - totalDiscount
                + cartTax.Amount
                + shipping.Amount
                + paymentFee.Amount);

            var totalConverted = _roundingHelper.Round(
                subtotalWithoutDiscount.Converted
                - subtotalDiscount.Converted
                - totalDiscountConverted
                + cartTax.Converted
                + shipping.Converted
                + paymentFee.Converted);

            // Applied gift cards.
            var appliedGiftCards = new List<AppliedGiftCard>();
            if (!cart.IncludesMatchingItems(x => x.IsRecurring))
            {
                var giftCards = await _giftCardService.GetValidGiftCardsAsync(_storeContext.CurrentStore.Id, customer);
                foreach (var gc in giftCards)
                {
                    var usableAmount = Round(gc.UsableAmount.Amount);

                    if (total > 0m)
                    {
                        var amount = total > usableAmount.Amount ? usableAmount.Amount : total;
                        total -= amount;

                        appliedGiftCards.Add(new()
                        {
                            GiftCard = gc.GiftCard,
                            UsableAmount = new(amount, _primaryCurrency)
                        });
                    }

                    if (totalConverted > 0m)
                    {
                        var amountConverted = totalConverted > usableAmount.Converted ? usableAmount.Converted : totalConverted;
                        totalConverted -= amountConverted;
                    }
                }
            }

            // Reward points.
            var rewardPoints = 0;
            var rewardPointsAmount = 0m;
            var rewardPointsAmountConverted = 0m;

            if (_rewardPointsSettings.Enabled &&
                includeRewardPoints &&
                total > 0m &&
                customer != null &&
                customer.GenericAttributes.UseRewardPointsDuringCheckout)
            {
                var points = customer.GetRewardPointsBalance();
                var pointsAmount = Round(ConvertRewardPointsToAmountInternal(points));

                if (total > pointsAmount.Amount)
                {
                    rewardPointsAmount = pointsAmount.Amount;
                    rewardPoints = points;
                }
                else
                {
                    rewardPointsAmount = total;
                    rewardPoints = ConvertAmountToRewardPoints(rewardPointsAmount);
                }

                rewardPointsAmountConverted = totalConverted > pointsAmount.Converted ? pointsAmount.Converted : totalConverted;
            }

            total = _roundingHelper.Round(Math.Max(total, 0m));
            totalConverted = _roundingHelper.Round(Math.Max(totalConverted, 0m));

            var creditBalanceToApply = 0m;
            var creditBalanceToApplyConverted = 0m;
            var toNearestRounding = 0m;
            var toNearestRoundingConverted = 0m;

            if (cartShipping != null)
            {
                total -= _roundingHelper.Round(rewardPointsAmount);
                totalConverted -= _roundingHelper.Round(rewardPointsAmountConverted);

                // Credit balance.
                if (includeCreditBalance && customer != null && total > 0m)
                {
                    var creditBalance = Round(customer.GenericAttributes.UseCreditBalanceDuringCheckout);
                    if (creditBalance.Amount > 0m)
                    {
                        if (creditBalance.Amount > total)
                        {
                            // Normalize used amount.
                            creditBalanceToApply = total;

                            customer.GenericAttributes.UseCreditBalanceDuringCheckout = total;
                            await _db.SaveChangesAsync();
                        }
                        else
                        {
                            creditBalanceToApply = creditBalance.Amount;
                        }
                    }

                    if (creditBalance.Converted > 0m)
                    {
                        creditBalanceToApplyConverted = creditBalance.Converted > totalConverted ? totalConverted : creditBalance.Converted;
                    }
                }

                total = _roundingHelper.Round(total - creditBalanceToApply);
                totalConverted = _roundingHelper.Round(totalConverted - creditBalanceToApplyConverted);

                // Round order total to nearest (cash rounding).
                if (_workingCurrency.RoundOrderTotalEnabled
                    && paymentMethodSystemName.HasValue()
                    && await _db.PaymentMethods.AnyAsync(x => x.PaymentMethodSystemName == paymentMethodSystemName && x.RoundOrderTotalEnabled))
                {
                    total = _roundingHelper.ToNearest(total, out toNearestRounding);
                    totalConverted = _roundingHelper.ToNearest(totalConverted, out toNearestRoundingConverted);
                }
            }

            var shoppingCartTotal = new ShoppingCartTotal
            {
                Total = cartShipping != null ? new(total, _primaryCurrency) : null,
                ToNearestRounding = new(toNearestRounding, _primaryCurrency),
                DiscountAmount = new(totalDiscount, _primaryCurrency),
                AppliedDiscount = appliedDiscount,
                RedeemedRewardPoints = rewardPoints,
                RedeemedRewardPointsAmount = new(rewardPointsAmount, _primaryCurrency),
                CreditBalance = new(creditBalanceToApply, _primaryCurrency),
                AppliedGiftCards = appliedGiftCards,
                LineItems = subtotal.LineItems,
                ConvertedAmount = new()
                {
                    Total = cartShipping != null ? new(totalConverted, _workingCurrency) : null,
                    ToNearestRounding = new(toNearestRoundingConverted, _workingCurrency)
                }
            };

            if (cache)
            {
                _requestCache.Put(cacheKey, shoppingCartTotal);
            }

            return shoppingCartTotal;
        }

        public virtual async Task<ShoppingCartSubtotal> GetShoppingCartSubtotalAsync(
            ShoppingCart cart,
            bool? includeTax = null,
            ProductBatchContext batchContext = null,
            bool activeOnly = false,
            bool cache = true)
        {
            includeTax ??= _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;

            if (activeOnly && cart.Items.Any(x => !x.Active))
            {
                cart = new(cart, cart.Items.Where(x => x.Active));
            }

            var cacheKey = $"ordercalculation:cartsubtotal:{cart.GetHashCode()}-{includeTax}";

            // INFO: CartSubtotalRule uses AsyncLock on this method! IRequestCache.Get would deadlock cart page.
            if (cache && _requestCache.Contains(cacheKey))
            {
                return _requestCache.Get<ShoppingCartSubtotal>(cacheKey, null);
            }

            var subtotal = await GetCartSubtotalAsync(cart, includeTax.Value, batchContext);
            var shoppingCartSubtotal = new ShoppingCartSubtotal
            {
                SubtotalWithoutDiscount = new(subtotal.SubtotalWithoutDiscount, _primaryCurrency),
                SubtotalWithDiscount = new(subtotal.SubtotalWithDiscount, _primaryCurrency),
                DiscountAmount = new(subtotal.DiscountAmount, _primaryCurrency),
                AppliedDiscount = subtotal.AppliedDiscount,
                TaxRates = subtotal.TaxRates,
                LineItems = subtotal.LineItems
            };

            if (cache)
            {
                _requestCache.Put(cacheKey, shoppingCartSubtotal);
            }

            return shoppingCartSubtotal;
        }

        public virtual async Task<ShoppingCartShippingTotal> GetShoppingCartShippingTotalAsync(ShoppingCart cart, bool? includeTax = null)
        {
            includeTax ??= _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;

            var result = await GetCartShippingTotalAsync(cart, includeTax.Value);
            if (result == null)
            {
                return new();
            }

            return new ShoppingCartShippingTotal
            {
                ShippingTotal = new(result.Tax.Price, _primaryCurrency),
                AppliedDiscount = result.AppliedDiscount,
                TaxRate = result.Tax.Rate.Rate
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

            if (!cart.IsShippingRequired)
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

        public virtual async Task<(bool Applied, Discount AppliedDiscount)> ApplyDiscountCouponAsync(ShoppingCart cart, string couponCode)
        {
            Guard.NotNull(cart);

            if (couponCode.IsEmpty() || cart.Customer.IsBot())
            {
                return (false, null);
            }

            couponCode = couponCode.Trim();

            var discount = await _db.Discounts.FirstOrDefaultAsync(x => x.CouponCode == couponCode);
            if (discount == null || !discount.RequiresCouponCode || !await _discountService.IsDiscountValidAsync(discount, cart.Customer, couponCode))
            {
                return (false, null);
            }

            var apply = true;
            var oldCouponCode = cart.Customer.GenericAttributes.DiscountCouponCode.NullEmpty();
            cart.Customer.GenericAttributes.DiscountCouponCode = couponCode;

            try
            {
                switch (discount.DiscountType)
                {
                    case DiscountType.AssignedToOrderTotal:
                        var cartTotal = await GetShoppingCartTotalAsync(cart);
                        apply = !cartTotal.Total.HasValue || discount.Id == cartTotal.AppliedDiscount?.Id;
                        break;
                    case DiscountType.AssignedToShipping:
                        var cartShipping = await GetShoppingCartShippingTotalAsync(cart);
                        apply = !cartShipping.ShippingTotal.HasValue || discount.Id == cartShipping.AppliedDiscount?.Id;
                        break;
                    default:
                        var cartSubtotal = await GetShoppingCartSubtotalAsync(cart);

                        if (discount.DiscountType == DiscountType.AssignedToOrderSubTotal)
                        {
                            apply = discount.Id == cartSubtotal.AppliedDiscount?.Id;
                        }
                        else
                        {
                            var appliedDiscountIds = cartSubtotal.LineItems
                                .SelectMany(x => x.Subtotal.AppliedDiscounts.Select(d => d.Id))
                                .ToArray();

                            apply = appliedDiscountIds.Contains(discount.Id);
                        }
                        break;
                }
            }
            finally
            {
                if (!apply)
                {
                    cart.Customer.GenericAttributes.DiscountCouponCode = oldCouponCode;
                }
            }

            return (apply, discount);
        }

        public virtual async Task<(Money Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(Money amount, DiscountType discountType, Customer customer)
        {
            var (discountAmount, appliedDiscount) = await GetDiscountAmountAsync(amount.Amount, discountType, customer);
            discountAmount = _roundingHelper.RoundIfEnabledFor(discountAmount);

            return (new(discountAmount, _primaryCurrency), appliedDiscount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Money ConvertRewardPointsToAmount(int rewardPoints)
            => new(ConvertRewardPointsToAmountInternal(rewardPoints), _primaryCurrency);

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

            var result = new CartSubtotal(includeTax);

            if (!cart.HasItems)
            {
                return result;
            }

            var customer = cart.Customer;
            var subtotalWithoutDiscountNet = 0m;
            var subtotalWithoutDiscountGross = 0m;

            batchContext ??= _productService.CreateProductBatchContext(cart.GetAllProducts(), null, customer, false);

            var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, customer, _primaryCurrency, batchContext);

            foreach (var cartItem in cart.Items.Where(x => x.Item.Product != null))
            {
                var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(cartItem, calculationOptions);
                var (unitPrice, subtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);

                //"- net:{0,10:N6} gross:{1,10:N6} final:{2,10:N6} excludingTax:{3}".FormatInvariant(
                //    subtotal.Tax.Value.PriceNet,
                //    subtotal.Tax.Value.PriceGross,
                //    subtotal.FinalPrice.Amount,
                //    !subtotal.Tax.Value.Inclusive).Dump();

                var tax = subtotal.Tax.Value;
                var priceNet = _roundingHelper.RoundIfEnabledFor(tax.PriceNet);
                var priceGross = _roundingHelper.RoundIfEnabledFor(tax.PriceGross);

                subtotalWithoutDiscountNet += priceNet;
                subtotalWithoutDiscountGross += priceGross;

                result.TaxRates.Add(tax.Rate.Rate, priceGross - priceNet);

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

                        subtotalWithoutDiscountNet += attributeTax.PriceNet;
                        subtotalWithoutDiscountGross += attributeTax.PriceGross;

                        result.TaxRates.Add(attributeTax.Rate.Rate, attributeTax.Amount);
                    }
                }
            }

            // Subtotal without discount.
            result.SubtotalWithoutDiscountNet = _roundingHelper.RoundIfEnabledFor(Math.Max(subtotalWithoutDiscountNet, 0m));
            result.SubtotalWithoutDiscountGross = _roundingHelper.RoundIfEnabledFor(Math.Max(subtotalWithoutDiscountGross, 0m));

            // We calculate discount amount on order subtotal excl tax (discount first).
            var (discountAmountNet, appliedDiscount) = await GetDiscountAmountAsync(subtotalWithoutDiscountNet, DiscountType.AssignedToOrderSubTotal, customer);

            if (subtotalWithoutDiscountNet < discountAmountNet)
            {
                discountAmountNet = subtotalWithoutDiscountNet;
            }

            var discountAmountGross = discountAmountNet;

            // Subtotal with discount net.
            var subtotalWithDiscountNet = subtotalWithoutDiscountNet - discountAmountNet;
            var subtotalWithDiscountGross = subtotalWithDiscountNet;

            // Add tax for shopping items & checkout attributes.
            var tempTaxRates = new Dictionary<decimal, decimal>(result.TaxRates);
            foreach (var kvp in tempTaxRates)
            {
                var taxRate = kvp.Key;
                var taxAmount = kvp.Value;

                if (taxAmount != 0m)
                {
                    // Discount the tax amount that applies to subtotal items.
                    if (subtotalWithoutDiscountNet > 0m)
                    {
                        var discountTax = result.TaxRates[taxRate] * (discountAmountNet / subtotalWithoutDiscountNet);
                        discountAmountGross += discountTax;
                        taxAmount = _roundingHelper.RoundIfEnabledFor(result.TaxRates[taxRate] - discountTax);
                        result.TaxRates[taxRate] = taxAmount;
                    }

                    subtotalWithDiscountGross += taxAmount;
                }
            }

            result.SubtotalWithDiscountNet = _roundingHelper.RoundIfEnabledFor(Math.Max(subtotalWithDiscountNet, 0m));
            result.SubtotalWithDiscountGross = _roundingHelper.RoundIfEnabledFor(Math.Max(subtotalWithDiscountGross, 0m));

            result.DiscountAmountNet = _roundingHelper.RoundIfEnabledFor(discountAmountNet);
            result.DiscountAmountGross = _roundingHelper.RoundIfEnabledFor(discountAmountGross);

            result.AppliedDiscount = appliedDiscount;

            return result;
        }

        protected virtual async Task<CartShipping> GetCartShippingTotalAsync(ShoppingCart cart, bool includeTax)
        {
            Guard.NotNull(cart);

            if (await IsFreeShippingAsync(cart))
            {
                return new() { Tax = Tax.Tax.Zero };
            }

            var (shippingTotalAmount, appliedDiscount) = await GetAdjustedShippingTotalAsync(cart);
            if (!shippingTotalAmount.HasValue)
            {
                return null;
            }

            var shippingAmount = _roundingHelper.RoundIfEnabledFor(Math.Max(shippingTotalAmount.Value, 0m));

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

            return new()
            {
                Tax = await _taxCalculator.CalculateShippingTaxAsync(shippingAmount, includeTax, taxCategoryId, cart.Customer),
                AppliedDiscount = appliedDiscount
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
            var shippingOption = cart.Customer?.GenericAttributes?.SelectedShippingOption;
            if (shippingOption != null)
            {
                var shippingMethods = await _shippingService.GetAllShippingMethodsAsync(cart.StoreId);

                return await AdjustShippingRateAsync(cart, shippingOption.Rate, shippingOption, shippingMethods);
            }

            await _db.LoadReferenceAsync(cart.Customer, x => x.ShippingAddress);

            // Use fixed rate (if possible).
            var shippingAddress = cart.Customer?.ShippingAddress ?? null;
            var shippingRateMethods = _shippingService.LoadEnabledShippingProviders(cart.StoreId)?.ToArray();

            if (shippingRateMethods.IsNullOrEmpty())
            {
                throw new InvalidOperationException(T("Shipping.CouldNotLoadMethod"));
            }

            if (shippingRateMethods.Length == 1)
            {
                var getShippingOptionRequest = _shippingService.CreateShippingOptionRequest(cart, shippingAddress, cart.StoreId);
                var fixedRate = await shippingRateMethods[0].Value.GetFixedRateAsync(getShippingOptionRequest);

                if (fixedRate.HasValue)
                {
                    // Ignore returned currency. The caller specifies it to avoid mixed currencies during calculation.
                    return await AdjustShippingRateAsync(cart, fixedRate.Value, null, null);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual decimal ConvertRewardPointsToAmountInternal(int rewardPoints)
            => _roundingHelper.Round(rewardPoints > 0 ? rewardPoints * _rewardPointsSettings.ExchangeRate : 0m);

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

        private (decimal Amount, decimal Converted) Round(decimal amount)
        {
            if (amount == 0m)
            {
                return (0m, 0m);
            }

            var roundedAmount = _roundingHelper.Round(amount);

            if (_primaryCurrency.Id == _workingCurrency.Id)
            {
                return (roundedAmount, roundedAmount);
            }

            return (roundedAmount, _roundingHelper.Round(_currencyService.ConvertToWorkingCurrency(amount).Amount));
        }

        #endregion

        #region Helpers

        protected class CartSubtotal
        {
            private readonly bool _includeTax;

            public CartSubtotal(bool includeTax)
            {
                _includeTax = includeTax;
            }

            public decimal SubtotalWithoutDiscountNet { get; set; }
            public decimal SubtotalWithoutDiscountGross { get; set; }
            public decimal SubtotalWithoutDiscount
                => _includeTax ? SubtotalWithoutDiscountGross : SubtotalWithoutDiscountNet;

            public decimal SubtotalWithDiscountNet { get; set; }
            public decimal SubtotalWithDiscountGross { get; set; }
            public decimal SubtotalWithDiscount
                => _includeTax ? SubtotalWithDiscountGross : SubtotalWithDiscountNet;

            public decimal DiscountAmountNet { get; set; }
            public decimal DiscountAmountGross { get; set; }
            public decimal DiscountAmount
                => _includeTax ? DiscountAmountGross : DiscountAmountNet;

            public Discount AppliedDiscount { get; set; }
            public TaxRatesDictionary TaxRates { get; set; } = new();
            public List<ShoppingCartLineItem> LineItems { get; set; } = new();
        }

        protected class CartShipping
        {
            public Tax.Tax Tax { get; set; }
            public Discount AppliedDiscount { get; set; }
        }

        protected class CartTaxingInfo
        {
            public Money SubtotalWithoutDiscount { get; internal set; }
            public bool HasHighestCartAmount { get; internal set; }
            public bool HasHighestTaxRate { get; internal set; }
            public decimal ProRataWeighting { get; internal set; } = 0m;
        }

        #endregion
    }
}
