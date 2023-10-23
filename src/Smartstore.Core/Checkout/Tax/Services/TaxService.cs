using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Data.Hooks;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Tax
{
    public partial class TaxService : AsyncDbSaveHook<TaxCategory>, ITaxService
    {
        const string DefaultTaxFormat = "{0} *";

        [GeneratedRegex("^(\\w{2})(.*)")]
        private static partial Regex VatNumberRegex();

        private readonly Dictionary<TaxRateCacheKey, TaxRate> _cachedTaxRates = new();
        private readonly Dictionary<TaxAddressKey, Address> _cachedTaxAddresses = new();
        private readonly IGeoCountryLookup _geoCountryLookup;
        private readonly IProviderManager _providerManager;
        private readonly IWorkContext _workContext;
        private readonly IRoundingHelper _roundingHelper;
        private readonly ILocalizationService _localizationService;
        private readonly TaxSettings _taxSettings;
        private readonly SmartDbContext _db;
        private readonly ViesTaxationHttpClient _client;

        public TaxService(
            SmartDbContext db,
            IGeoCountryLookup geoCountryLookup,
            IProviderManager providerManager,
            IWorkContext workContext,
            IRoundingHelper roundingHelper,
            ILocalizationService localizationService,
            TaxSettings taxSettings,
            ViesTaxationHttpClient client)
        {
            _db = db;
            _geoCountryLookup = geoCountryLookup;
            _providerManager = providerManager;
            _workContext = workContext;
            _roundingHelper = roundingHelper;
            _localizationService = localizationService;
            _taxSettings = taxSettings;
            _client = client;
        }

        #region Hook 

        protected override Task<HookResult> OnDeletedAsync(TaxCategory entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedTaxCategoryIds = entries
                .Where(x => x.InitialState == EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<TaxCategory>()
                .Select(x => x.Id)
                .ToList();

            if (deletedTaxCategoryIds.Count > 0)
            {
                var newTaxCategoryId = await _db.TaxCategories
                    .OrderBy(x => x.DisplayOrder)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync(cancelToken);

                await _db.Products
                    .Where(x => deletedTaxCategoryIds.Contains(x.TaxCategoryId))
                    .ExecuteUpdateAsync(x => x.SetProperty(p => p.TaxCategoryId, p => newTaxCategoryId), cancelToken);
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Provider<ITaxProvider> LoadActiveTaxProvider()
            => LoadTaxProviderBySystemName(_taxSettings.ActiveTaxProviderSystemName) ?? LoadAllTaxProviders().FirstOrDefault();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Provider<ITaxProvider> LoadTaxProviderBySystemName(string systemName)
            => _providerManager.GetProvider<ITaxProvider>(systemName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual IEnumerable<Provider<ITaxProvider>> LoadAllTaxProviders()
            => _providerManager.GetAllProviders<ITaxProvider>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual string FormatTaxRate(decimal taxRate)
            => taxRate.ToString("G29");

        public virtual async Task<TaxRate> GetTaxRateAsync(Product product, int? taxCategoryId = null, Customer customer = null)
        {
            taxCategoryId ??= product?.TaxCategoryId ?? 0;
            customer ??= _workContext.CurrentCustomer;

            var cacheKey = CreateTaxRateCacheKey(customer, taxCategoryId.Value, product);
            if (!_cachedTaxRates.TryGetValue(cacheKey, out var result))
            {
                result = await GetTaxRateCore(product, taxCategoryId.Value, customer);
                _cachedTaxRates[cacheKey] = result;
            }

            return result;
        }

        protected virtual async Task<TaxRate> GetTaxRateCore(Product product, int taxCategoryId, Customer customer)
        {
            var activeTaxProvider = LoadActiveTaxProvider();
            if (activeTaxProvider == null || await IsTaxExemptAsync(product, customer))
            {
                return new(0m, taxCategoryId);
            }

            var request = new TaxRateRequest
            {
                Customer = customer,
                TaxCategoryId = taxCategoryId,
                Address = await GetTaxAddressAsync(customer, product)
            };

            return await activeTaxProvider.Value.GetTaxRateAsync(request);
        }

        public virtual async Task<VatCheckResult> GetVatNumberStatusAsync(string fullVatNumber)
        {
            if (fullVatNumber.IsEmpty())
            {
                return new(VatNumberStatus.Empty, fullVatNumber);
            }

            // DE 111 1111 111 or DE1111111111
            // More advanced regex - https://forum.codeigniter.com/thread-31835.html
            // This regex only checks whether the first two chars are alphanumeric...
            var match = VatNumberRegex().Match(fullVatNumber.Trim());
            if (!match.Success)
            {
                return new(VatNumberStatus.Invalid, fullVatNumber);
            }

            var twoLetterIsoCode = match.Groups[1].Value;
            var vatNumber = match.Groups[2].Value;

            if (twoLetterIsoCode.IsEmpty() || vatNumber.IsEmpty())
            {
                return new(VatNumberStatus.Empty, fullVatNumber);
            }

            if (!_taxSettings.EuVatUseWebService)
            {
                return new(VatNumberStatus.Unknown, fullVatNumber);
            }

            try
            {
                var response = await _client.CheckVatAsync(vatNumber.Replace(" ", string.Empty), twoLetterIsoCode.ToUpper());

                return new(response.IsValid ? VatNumberStatus.Valid : VatNumberStatus.Invalid, fullVatNumber)
                {
                    Name = response.Name,
                    Address = response.Address,
                    CountryCode = response.CountryCode,
                };
            }
            catch (Exception ex)
            {
                return new(VatNumberStatus.Unknown, fullVatNumber)
                {
                    Exception = ex
                };
            }
        }

        public virtual async Task<bool> IsTaxExemptAsync(Product product, Customer customer)
        {
            if (customer != null)
            {
                if (customer.IsTaxExempt)
                    return true;

                await _db.LoadCollectionAsync(customer, x => x.CustomerRoleMappings, false, q => q.Include(x => x.CustomerRole));

                if (customer.CustomerRoleMappings.Select(x => x.CustomerRole).Where(x => x.Active).Any(x => x.TaxExempt))
                    return true;
            }

            return product?.IsTaxExempt ?? false;
        }

        public virtual async Task<bool> IsVatExemptAsync(Customer customer, Address address = null)
        {
            if (!_taxSettings.EuVatEnabled || customer is null)
            {
                return false;
            }

            address ??= await GetTaxAddressAsync(customer);
            if (address?.Country is null)
            {
                return false;
            }

            if (!address.Country.SubjectToVat)
            {
                // VAT not chargeable if shipping outside VAT zone.
                return true;
            }

            // VAT not chargeable if address, customer and config meet our VAT exemption requirements:
            // returns true if this customer is VAT exempt because they are shipping within the EU but outside our shop country, 
            // they have supplied a validated VAT number and the shop is configured to allow VAT exemption.
            if (address.CountryId == _taxSettings.EuVatShopCountryId)
            {
                return false;
            }

            return customer.VatNumberStatusId == (int)VatNumberStatus.Valid && _taxSettings.EuVatAllowVatExemption;
        }

        public virtual string GetTaxFormat(
            bool? displayTaxSuffix = null,
            bool? priceIncludesTax = null,
            PricingTarget target = PricingTarget.Product,
            Language language = null)
        {
            displayTaxSuffix ??= target == PricingTarget.Product
                ? _taxSettings.DisplayTaxSuffix
                : (target == PricingTarget.ShippingCharge
                    ? _taxSettings.DisplayTaxSuffix && _taxSettings.ShippingIsTaxable
                    : _taxSettings.DisplayTaxSuffix && _taxSettings.PaymentMethodAdditionalFeeIsTaxable);

            if (displayTaxSuffix == true)
            {
                // Show tax suffix.
                priceIncludesTax ??= _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
                language ??= _workContext.WorkingLanguage;

                string resource = _localizationService.GetResource(priceIncludesTax.Value ? "Products.InclTaxSuffix" : "Products.ExclTaxSuffix", language.Id, false);
                var postFormat = resource.NullEmpty() ?? DefaultTaxFormat;

                return postFormat;
            }
            else
            {
                return null;
            }
        }

        #region Utilities

        /// <summary>
        /// Creates tax rate cache key as tuple of {int, int, int}.
        /// </summary>
        /// <param name="customer">Customer. Gets id or 0 if <c>null</c>.</param>
        /// <param name="taxCategoryId">Tax category identifier.</param>
        /// <param name="product">Product. Gets id or 0 if <c>null</c>.</param>
        private static TaxRateCacheKey CreateTaxRateCacheKey(Customer customer, int taxCategoryId, Product product)
            => new(customer?.Id ?? 0, taxCategoryId, product?.Id ?? 0);

        /// <summary>
        /// Calculates a tax based price.
        /// </summary>
        /// <param name="price">Original price.</param>
        /// <param name="percent">Percentage to change.</param>
        /// <param name="increase"><c>true</c> to increase and <c>false</c> to decrease the price.</param>
        /// <returns>Calculated price.</returns>
        protected virtual decimal CalculateAmount(decimal price, decimal percent, bool increase)
        {
            if (percent == decimal.Zero)
            {
                return price;
            }

            var result = increase
                ? price * (1 + percent / 100)
                : price - (price / (100 + percent) * percent);

            return result;
        }

        protected virtual async Task<(decimal Amount, decimal TaxRate)> GetProductPriceAmountAsync(
            Product product,
            decimal price,
            bool? inclusive = null,
            bool? isGrossPrice = null,
            int? taxCategoryId = null,
            Customer customer = null)
        {
            // Don't calculate if price is 0.
            if (price == decimal.Zero)
            {
                return (price, decimal.Zero);
            }

            customer ??= _workContext.CurrentCustomer;
            taxCategoryId ??= product?.TaxCategoryId;
            inclusive ??= _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;

            var taxRate = await GetTaxRateAsync(product, taxCategoryId, customer);

            if (isGrossPrice ?? _taxSettings.PricesIncludeTax)
            {
                if (!inclusive.Value)
                {
                    return (CalculateAmount(price, taxRate, false), taxRate);
                }
            }
            else
            {
                if (inclusive.Value)
                {
                    return (CalculateAmount(price, taxRate, true), taxRate);
                }
            }

            // Gross > Net RoundFix
            price = _roundingHelper.RoundIfEnabledFor(price);

            return (price, taxRate);
        }

        /// <summary>
        /// Checks whether the customer is a consumer (NOT a company) within the EU.
        /// </summary>
        /// <param name="customer">Customer to check.</param>
        /// <remarks>
        /// A customer is assumed to be an EU consumer if the default tax address does not contain a company name, 
        /// OR the IP address is within the EU, 
        /// OR a business name has been provided but the EU VAT number is invalid.
        /// </remarks>
        /// <returns>
        /// <c>True</c> if the customer is a consumer within the EU, <c>False</c> if otherwise.
        /// </returns>
        protected virtual bool IsEuConsumer(Customer customer)
        {
            if (customer == null)
            {
                return false;
            }

            // If BillingAddress is explicitly set but no company is specified, we assume that it is a consumer.
            var address = customer.BillingAddress;
            if (address != null && address.Company.IsEmpty())
            {
                return true;
            }

            // Otherwise check whether customer's IP country is in the EU.
            var isInEu = _geoCountryLookup.LookupCountry(customer.LastIpAddress)?.IsInEu == true;
            if (!isInEu)
            {
                return false;
            }

            // Companies with an invalid VAT number are assumed to be consumers.
            return customer.VatNumberStatusId != (int)VatNumberStatus.Valid;
        }

        /// <summary>
        /// Gets tax address of customer.
        /// </summary>
        /// <param name="customer">Customer of tax address.</param>
        /// <param name="product">The related product is used for caching and ESD check. Can be <c>null</c>.</param>
        /// <remarks>
        /// Tries to get customer address from cached addresses before accessing database.
        /// </remarks>
        /// <returns>
        /// Customer's tax address.
        /// </returns>
        protected virtual async Task<Address> GetTaxAddressAsync(Customer customer, Product product = null)
        {
            Guard.NotNull(customer);

            var productIsEsd = product?.IsEsd ?? false;
            var cacheKey = new TaxAddressKey(customer.Id, productIsEsd);

            if (_cachedTaxAddresses.TryGetValue(cacheKey, out var address))
                return address;

            // According to the EU VAT regulations for electronic services from 2015,            
            // VAT must be charged in the EU country from which the customer originates (BILLING address).
            // In addition, the origin of the IP addresses should also be checked for verification.
            var basedOn = _taxSettings.TaxBasedOn;

            if ((_taxSettings.EuVatEnabled && productIsEsd && IsEuConsumer(customer)) ||
                (basedOn == TaxBasedOn.ShippingAddress && customer.ShippingAddress == null))
            {
                basedOn = TaxBasedOn.BillingAddress;
            }

            if (basedOn == TaxBasedOn.BillingAddress && customer.BillingAddress == null)
            {
                basedOn = TaxBasedOn.DefaultAddress;
            }

            address = basedOn switch
            {
                TaxBasedOn.BillingAddress => customer.BillingAddress,
                TaxBasedOn.ShippingAddress => customer.ShippingAddress,
                _ => await _db.Addresses.FindByIdAsync(_taxSettings.DefaultTaxAddressId),
            };

            _cachedTaxAddresses[cacheKey] = address;

            return address;
        }

        #endregion

        private class TaxAddressKey : Tuple<int, bool>
        {
            public TaxAddressKey(int customerId, bool productIsEsd)
                : base(customerId, productIsEsd)
            {
            }
        }

        private class TaxRateCacheKey : Tuple<int, int, int>
        {
            public TaxRateCacheKey(int customerId, int taxCategoryId, int variantId)
                : base(customerId, taxCategoryId, variantId)
            {
            }
        }
    }
}