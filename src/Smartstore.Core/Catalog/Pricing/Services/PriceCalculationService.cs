using System.Linq.Dynamic.Core;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Pricing
{
    public partial class PriceCalculationService : IPriceCalculationService
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IPriceCalculatorFactory _calculatorFactory;
        private readonly ITaxCalculator _taxCalculator;
        private readonly IProductService _productService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly IRoundingHelper _roundingHelper;
        private readonly IPriceLabelService _priceLabelService;
        private readonly PriceSettings _priceSettings;
        private readonly CurrencySettings _currencySettings;
        private readonly TaxSettings _taxSettings;
        private readonly Currency _primaryCurrency;

        public PriceCalculationService(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext,
            IPriceCalculatorFactory calculatorFactory,
            ITaxCalculator taxCalculator,
            IProductService productService,
            IProductAttributeMaterializer productAttributeMaterializer,
            ITaxService taxService,
            ICurrencyService currencyService,
            IRoundingHelper roundingHelper,
            IPriceLabelService priceLabelService,
            PriceSettings priceSettings,
            CurrencySettings currencySettings,
            TaxSettings taxSettings)
        {
            _db = db;
            _workContext = workContext;
            _storeContext = storeContext;
            _calculatorFactory = calculatorFactory;
            _taxCalculator = taxCalculator;
            _productService = productService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _taxService = taxService;
            _currencyService = currencyService;
            _roundingHelper = roundingHelper;
            _priceLabelService = priceLabelService;
            _priceSettings = priceSettings;
            _currencySettings = currencySettings;
            _taxSettings = taxSettings;

            _primaryCurrency = currencyService.PrimaryCurrency;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual PriceCalculationOptions CreateDefaultOptions(
            bool forListing,
            Customer customer = null,
            Currency targetCurrency = null,
            ProductBatchContext batchContext = null)
        {
            customer ??= batchContext?.Customer ?? _workContext.CurrentCustomer;

            var store = batchContext?.Store ?? _storeContext.CurrentStore;
            var language = _workContext.WorkingLanguage;
            var priceDisplay = _priceSettings.PriceDisplayType;
            var taxInclusive = _workContext.GetTaxDisplayTypeAsync(customer, store.Id).Await() == TaxDisplayType.IncludingTax;
            var determinePreselectedPrice = forListing && priceDisplay == PriceDisplayType.PreSelectedPrice;

            targetCurrency ??= _workContext.WorkingCurrency;
            batchContext ??= _productService.CreateProductBatchContext(null, store, customer, false);

            var options = new PriceCalculationOptions(batchContext, customer, store, language, targetCurrency)
            {
                IsGrossPrice = _taxSettings.PricesIncludeTax,
                TaxInclusive = taxInclusive,
                IgnorePercentageDiscountOnTierPrices = !_priceSettings.ApplyPercentageDiscountOnTierPrice,
                IgnorePercentageTierPricesOnAttributePriceAdjustments = !_priceSettings.ApplyTierPricePercentageToAttributePriceAdjustments,
                IgnoreDiscounts = forListing && priceDisplay == PriceDisplayType.PriceWithoutDiscountsAndAttributes,
                DetermineLowestPrice = forListing && priceDisplay == PriceDisplayType.LowestPrice,
                DeterminePreselectedPrice = determinePreselectedPrice,
                ApplyPreselectedAttributes = determinePreselectedPrice,
                TaxFormat = !forListing ? null : _taxService.GetTaxFormat(null, taxInclusive, PricingTarget.Product, language),
                PriceRangeFormat = T("Products.PriceRangeFrom").Value,
                RoundingCurrency = targetCurrency == _primaryCurrency ? _workContext.WorkingCurrency : targetCurrency
            };

            return options;
        }

        public virtual async Task<PriceCalculationContext> CreateCalculationContextAsync(OrganizedShoppingCartItem cartItem, PriceCalculationOptions options)
        {
            Guard.NotNull(cartItem);
            Guard.NotNull(options);

            var product = cartItem.Item.Product;
            var context = new PriceCalculationContext(product, cartItem.Item.Quantity, options)
            {
                CartItem = cartItem
            };

            // Include attributes selected for this cart item in price calculation.
            context.AddSelectedAttributes(cartItem);

            // Include bundle item data if the cart item is a bundle item.
            if (cartItem.Item.BundleItem != null)
            {
                context.BundleItem = cartItem.Item.BundleItem;
            }

            // Perf: we already have the bundle items of a bundled product. No need to load them again during calculation.
            if (cartItem.ChildItems?.Any() ?? false)
            {
                context.BundleItems = cartItem.ChildItems
                    .Where(x => x.Item.BundleItem != null)
                    .Select(x => x.Item.BundleItem)
                    .ToList();
            }

            if (product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing)
            {
                Guard.NotNull(cartItem.ChildItems);

                foreach (var bundleItem in cartItem.ChildItems)
                {
                    await _productAttributeMaterializer.MergeWithCombinationAsync(bundleItem.Item.Product, bundleItem.Item.AttributeSelection);
                }
            }
            else
            {
                await _productAttributeMaterializer.MergeWithCombinationAsync(product, cartItem.Item.AttributeSelection);
            }

            return context;
        }

        public virtual async Task<CalculatedPrice> CalculatePriceAsync(PriceCalculationContext context)
        {
            Guard.NotNull(context);

            // Remember source product.
            var product = context.Product;
            var calculatorContext = await RunCalculators(context);
            var unitPrice = await CreateCalculatedPrice(calculatorContext, product);

            return unitPrice;
        }

        public virtual async Task<(CalculatedPrice UnitPrice, CalculatedPrice Subtotal)> CalculateSubtotalAsync(PriceCalculationContext context)
        {
            Guard.NotNull(context);

            var product = context.Product;
            var calculatorContext = await RunCalculators(context);
            var price = await CreateCalculatedPrice(calculatorContext, product);

            if (context.Quantity <= 1)
            {
                return (price, price);
            }

            var subtotal = await CreateCalculatedPrice(calculatorContext, product, context.Quantity);
            return (price, subtotal);
        }

        public virtual async Task<Money> CalculateProductCostAsync(Product product, ProductVariantAttributeSelection selection = null)
        {
            Guard.NotNull(product);
            Guard.NotNull(selection);

            var productCost = product.ProductCost;

            if (selection != null)
            {
                var attributeValues = await _productAttributeMaterializer.MaterializeProductVariantAttributeValuesAsync(selection);

                var productLinkageValues = attributeValues
                    .Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage && x.LinkedProductId != 0)
                    .ToList();
                var linkedProductIds = productLinkageValues
                    .Select(x => x.LinkedProductId)
                    .Distinct()
                    .ToArray();

                if (linkedProductIds.Any())
                {
                    var linkedProducts = await _db.Products
                        .AsNoTracking()
                        .Where(x => linkedProductIds.Contains(x.Id))
                        .Select(x => new { x.Id, x.ProductCost })
                        .ToListAsync();
                    var linkedProductsDic = linkedProducts.ToDictionarySafe(x => x.Id, x => x.ProductCost);

                    foreach (var value in productLinkageValues)
                    {
                        if (linkedProductsDic.TryGetValue(value.LinkedProductId, out var linkedProductCost))
                        {
                            productCost += linkedProductCost * value.Quantity;
                        }
                    }
                }
            }

            return new(productCost, _primaryCurrency);
        }

        public virtual string GetBasePriceInfo(
            Product product,
            Money price,
            Currency targetCurrency = null,
            Language language = null,
            bool includePackageContentPerUnit = true,
            bool? displayTaxSuffix = null)
        {
            Guard.NotNull(product);

            if (!product.BasePriceHasValue || product.BasePriceAmount == 0)
            {
                return string.Empty;
            }

            var basePriceAmount = Convert.ToDecimal((price / product.BasePriceAmount) * product.BasePriceBaseAmount);
            var basePrice = _taxService.ApplyTaxFormat(
                new Money(basePriceAmount, targetCurrency ?? _workContext.WorkingCurrency),
                displayTaxSuffix,
                null,
                language);

            if (includePackageContentPerUnit)
            {
                var packageContentPerUnit = product.BasePriceAmount.Value.ToString("G29");

                return T("Products.BasePriceInfo").Value.FormatInvariant(
                    packageContentPerUnit,
                    product.BasePriceMeasureUnit,
                    basePrice,
                    product.BasePriceBaseAmount);
            }
            else
            {
                return T("Products.BasePriceInfo.LanguageInsensitive").Value.FormatInvariant(
                    basePrice,
                    product.BasePriceBaseAmount,
                    product.BasePriceMeasureUnit);
            }
        }

        #region Utilities

        protected virtual async Task<CalculatorContext> RunCalculators(PriceCalculationContext context)
        {
            // Remember source product.
            var product = context.Product;

            // Handle cases where not to run any calculator.
            if (product.CustomerEntersPrice)
            {
                return new(context, context.CartItem?.Item?.CustomerEnteredPrice ?? 0m, PricingType.CustomerEnteredPrice);
            }
            else if (product.CallForPrice)
            {
                return new(context, 0m, PricingType.CallForPrice);
            }

            // Collect calculators
            var calculators = _calculatorFactory.GetCalculators(context);
            var calculatorContext = new CalculatorContext(context, product.Price);

            // Run all collected calculators
            await _calculatorFactory.RunCalculators(calculators, calculatorContext);

            return calculatorContext;
        }

        protected virtual void DetectComparePrices(CalculatorContext context, CalculatedPrice result, TaxRate taxRate)
        {
            var product = context.Product;
            var retailPrice = (decimal?)null;
            var hasComparePrice = product.ComparePrice > 0;
            var hasDiscount = context.DiscountAmount > 0 || context.AppliedTierPrice != null || (context.OfferPrice.HasValue && context.OfferPrice < product.Price);

            PriceLabel comparePriceLabel = null;

            if (hasComparePrice)
            {
                comparePriceLabel = _priceLabelService.GetComparePriceLabel(product);
                if (comparePriceLabel.IsRetailPrice && product.ComparePrice > product.Price)
                {
                    if (product.ProductType != ProductType.BundledProduct || !product.BundlePerItemPricing)
                    {
                        // A bundle with computed item pricing cannot have a retail price
                        retailPrice = product.ComparePrice;
                        result.RetailPriceLabel = comparePriceLabel;
                        result.RetailPrice = ConvertAmount(product.ComparePrice, context, taxRate, false, out _);
                    }
                }
            }

            if (hasComparePrice && product.ComparePrice > context.FinalPrice && retailPrice == null && !hasDiscount)
            {
                // A compare price which is NOT the retail price is the regular price (if no discounts are present)
                result.RegularPriceLabel = comparePriceLabel;
                result.RegularPrice = ConvertAmount(product.ComparePrice, context, taxRate, false, out _);
                return;
            }

            var regularPrice = GetRegularPrice(context);
            if (regularPrice != null && regularPrice != retailPrice && regularPrice > context.FinalPrice)
            {
                // Apply regular price if it's not the retail price and greater than the final price.
                result.RegularPriceLabel = _priceLabelService.GetRegularPriceLabel(product);
                result.RegularPrice = ConvertAmount(regularPrice, context, taxRate, false, out _);
            }
        }

        protected virtual decimal? GetRegularPrice(CalculatorContext context)
        {
            var product = context.Product;
            var regularPrice = (decimal?)null;
            var hasComparePrice = product.ComparePrice > 0;
            var hasOfferPrice = context.OfferPrice.HasValue;
            var hasTierPrice = context.AppliedTierPrice != null;
            var price = context.RegularPrice;

            if (hasOfferPrice && _priceSettings.OfferPriceReplacesRegularPrice && !hasTierPrice)
            {
                // Hide regular price, unless there is a tier price (higher priority).
                return null;
            }

            if (context.DiscountAmount > 0 || hasTierPrice)
            {
                if (hasOfferPrice)
                {
                    regularPrice = Math.Min(context.OfferPrice.Value, hasComparePrice ? product.ComparePrice : price);
                }
                else
                {
                    regularPrice = hasComparePrice ? Math.Min(product.ComparePrice, price) : price;
                }
            }

            if (regularPrice == null && hasOfferPrice)
            {
                // PAngV: price would not be allowed if greater than compare price.
                regularPrice = hasComparePrice ? Math.Min(product.ComparePrice, price) : price;
            }

            if (regularPrice == null && hasComparePrice && product.ComparePrice > price)
            {
                regularPrice = product.ComparePrice;
            }

            return regularPrice;
        }

        protected virtual async Task<CalculatedPrice> CreateCalculatedPrice(CalculatorContext context, Product product, int subtotalQuantity = 1)
        {
            var options = context.Options;

            // Determine tax rate for product.
            var taxRate = await _taxService.GetTaxRateAsync(context.Product, null, options.Customer);

            var endDates = context.AppliedDiscounts.Select(x => x.EndDateUtc)
                .Concat(new[] { context.OfferEndDateUtc })
                .Where(x => x.HasValue && x > DateTime.UtcNow)
                .ToArray();

            // Prepare result by converting price amounts.
            var result = new CalculatedPrice(context)
            {
                Product = context.Product,
                ValidUntilUtc = endDates.Min(),
                OfferPrice = ConvertAmount(context.OfferPrice, context, taxRate, false, out _, subtotalQuantity),
                PreselectedPrice = ConvertAmount(context.PreselectedPrice, context, taxRate, false, out _, subtotalQuantity),
                LowestPrice = ConvertAmount(context.LowestPrice, context, taxRate, false, out _, subtotalQuantity),
                DiscountAmount = ConvertAmount(context.DiscountAmount, context, taxRate, false, out _, subtotalQuantity).Value,
                FinalPrice = ConvertAmount(context.FinalPrice, context, taxRate, true, out var tax, subtotalQuantity).Value,
                Tax = tax
            };

            if (tax.HasValue && _primaryCurrency != options.TargetCurrency)
            {
                var convertedAmount = _currencyService.ConvertFromPrimaryCurrency(context.FinalPrice, options.TargetCurrency).Amount;

                result.Tax = CalculateTax(options, convertedAmount, taxRate, subtotalQuantity);
            }

            // Convert attribute price adjustments.
            context.AttributePriceAdjustments.Each(x => x.Price = ConvertAmount(x.RawPriceAdjustment, context, taxRate, false, out _).Value);

            // Detect retail & regular price.
            DetectComparePrices(context, result, taxRate);

            // Saving price.
            var savingPrice = result.RegularPrice ?? result.RetailPrice ?? Money.Zero;
            var hasSaving = savingPrice > 0 && result.FinalPrice < savingPrice;

            result.Saving = new()
            {
                HasSaving = hasSaving,
                SavingPrice = savingPrice,
                SavingPercent = hasSaving ? (float)((savingPrice - result.FinalPrice) / savingPrice) * 100 : 0f,
                SavingAmount = hasSaving ? (savingPrice - result.FinalPrice).WithPostFormat(null) : null
            };

            // In product lists, show the base price of the preselected attribute combination (instead of the base price set on product level).
            var ac = context.AppliedAttributeCombination;
            if (ac != null
                && (ac.BasePriceAmount.HasValue || ac.BasePriceBaseAmount.HasValue)
                && _priceSettings.ShowBasePriceInProductLists)
            {
                product.MergedDataValues ??= [];

                if (ac.BasePriceAmount.HasValue)
                    product.MergedDataValues["BasePriceAmount"] = ac.BasePriceAmount.Value;

                if (ac.BasePriceBaseAmount.HasValue)
                    product.MergedDataValues["BasePriceBaseAmount"] = ac.BasePriceBaseAmount.Value;
            }

            return result;
        }

        protected virtual Money? ConvertAmount(decimal? amount, CalculatorContext context, TaxRate taxRate, bool isFinalPrice, out Tax? tax, int subtotalQuantity = 1)
        {
            if (amount == null)
            {
                tax = null;
                return null;
            }

            var options = context.Options;

            // A product price cannot be less than zero.
            if (amount < 0)
            {
                amount = 0;
            }

            tax = CalculateTax(options, amount.Value, taxRate, subtotalQuantity);
            amount = tax.Value.Price;

            var money = _currencyService.ConvertFromPrimaryCurrency(amount.Value, options.TargetCurrency);

            if (context.PricingType == PricingType.CallForPrice)
            {
                money = money.WithPostFormat(T("Products.CallForPrice"));
            }
            else if (amount != 0 && options.TaxFormat != null)
            {
                money = money.WithPostFormat(options.TaxFormat);
            }

            if (isFinalPrice && context.HasPriceRange)
            {
                var finalPricePostFormat = money.PostFormat;
                finalPricePostFormat = finalPricePostFormat == null
                    ? options.PriceRangeFormat
                    : string.Format(options.PriceRangeFormat, finalPricePostFormat);

                if (money.PostFormat != finalPricePostFormat)
                {
                    money = money.WithPostFormat(finalPricePostFormat);
                }
            }

            return money;
        }

        protected virtual Tax CalculateTax(PriceCalculationOptions options, decimal amount, TaxRate taxRate, int subtotalQuantity = 1)
        {
            var roundingCurrency = options.RoundingCurrency;

            if (subtotalQuantity > 1)
            {
                var taxDisplayType = options.TaxInclusive ? TaxDisplayType.IncludingTax : TaxDisplayType.ExcludingTax;

                amount = roundingCurrency.RoundUnitPrices ?? _currencySettings.RoundUnitPrices
                    ? _roundingHelper.RoundIfEnabledFor(amount, roundingCurrency, taxDisplayType) * subtotalQuantity
                    : _roundingHelper.RoundIfEnabledFor(amount * subtotalQuantity, roundingCurrency, taxDisplayType);
            }

            return options.IsGrossPrice
                 ? _taxCalculator.CalculateTaxFromGross(amount, taxRate, options.TaxInclusive, roundingCurrency)
                 : _taxCalculator.CalculateTaxFromNet(amount, taxRate, options.TaxInclusive, roundingCurrency);
        }

        //private Tax CalculateTax_Old(PriceCalculationOptions options, decimal amount, TaxRate taxRate, int subtotalQuantity = 1)
        //{
        //    var roundingCurrency = options.RoundingCurrency;

        //    if (subtotalQuantity <= 1)
        //    {
        //        // Round.
        //        return options.IsGrossPrice
        //             ? _taxCalculator.CalculateTaxFromGross(amount, taxRate, options.TaxInclusive, roundingCurrency)
        //             : _taxCalculator.CalculateTaxFromNet(amount, taxRate, options.TaxInclusive, roundingCurrency);
        //    }

        //    // Do not round.
        //    var t = options.IsGrossPrice
        //         ? _taxCalculator.CalculateTaxFromGross(amount, taxRate, options.TaxInclusive)
        //         : _taxCalculator.CalculateTaxFromNet(amount, taxRate, options.TaxInclusive);

        //    // Round.
        //    return new(
        //        t.Rate,
        //        t.Amount * subtotalQuantity,
        //        GetSubtotal(t.Price),
        //        GetSubtotal(t.PriceNet),
        //        GetSubtotal(t.PriceGross),
        //        t.IsGrossPrice,
        //        t.Inclusive);

        //    decimal GetSubtotal(decimal value)
        //    {
        //        return roundingCurrency.RoundUnitPrices ?? _currencySettings.RoundUnitPrices
        //            ? _roundingHelper.RoundIfEnabledFor(value, roundingCurrency) * subtotalQuantity
        //            : _roundingHelper.RoundIfEnabledFor(value * subtotalQuantity, roundingCurrency);
        //    }
        //}

        #endregion
    }
}
