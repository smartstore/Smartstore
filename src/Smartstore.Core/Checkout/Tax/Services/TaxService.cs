using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Data;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Tax
{
    /// <summary>
    /// Tax service
    /// </summary>
    public partial class TaxService : ITaxService
    {
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

        private readonly Dictionary<TaxRateCacheKey, decimal> _cachedTaxRates = new();
        private readonly Dictionary<TaxAddressKey, Address> _cachedTaxAddresses = new();
        private readonly IGeoCountryLookup _geoCountryLookup;
        private readonly IProviderManager _providerManager;
        private readonly IWorkContext _workContext;
        private readonly TaxSettings _taxSettings;
        private readonly SmartDbContext _db;

        public TaxService(
            IGeoCountryLookup geoCountryLookup,
            IProviderManager providerManager,
            IWorkContext workContext,
            TaxSettings taxSettings,
            SmartDbContext db)
        {
            _geoCountryLookup = geoCountryLookup;
            _providerManager = providerManager;
            _workContext = workContext;
            _taxSettings = taxSettings;
            _db = db;
        }

        /// <summary>
        /// Creates tax rate cache key as tuple of <int, int, int>
        /// </summary>
        /// <param name="customer">Customer as identifier. Gets id of 0 if <c>null</c></param>
        /// <param name="taxCategoryId">Tax category identifier</param>
        /// <param name="product">Product as identifier. Gets id of 0 if <c>null</c></param>
        /// <returns></returns>
        private static TaxRateCacheKey CreateTaxRateCacheKey(Customer customer, int taxCategoryId, Product product)
            => new(customer?.Id ?? 0, taxCategoryId, product?.Id ?? 0);

        /// <summary>
        /// Gets recalculated price for currency
        /// </summary>
        /// <param name="price">Original price</param>
        /// <param name="percent">Percentage to change</param>
        /// <param name="increase">Whether it is an increase or not</param>
        /// <param name="currency">Conversion currency to convert to</param>
        /// <remarks>
        /// Rounds result if enabled for currency
        /// </remarks>
        /// <returns>
        /// Price converted to currency
        /// </returns>
        protected static decimal CalculatePrice(decimal price, decimal percent, bool increase, Currency currency)
        {
            Guard.NotNull(currency, nameof(currency));

            if (percent == decimal.Zero)
                return price;

            var percentageFactor = percent / 100 + 1;
            var result = increase
                ? price * percentageFactor
                : price / percentageFactor;

            return currency.RoundIfEnabledFor(result);
        }

        /// <summary>
        /// Creates a tax calculation request
        /// </summary>
        /// <param name="customer">Customer used for tax calculation</param>
        /// <param name="taxCategoryId">Tax category identifier. If it is 0, the product can be used to determine the tax category identifier</param>
        /// <param name="product">Product used for tax calculation. Can be <c>null</c></param>
        /// <returns>Calculate tax request object</returns>
        protected async Task<CalculateTaxRequest> CreateCalculateTaxRequestAsync(Customer customer, int taxCategoryId, Product product)
        {
            Guard.NotNull(customer, nameof(customer));

            taxCategoryId = taxCategoryId > 0
                ? taxCategoryId
                : product?.TaxCategoryId ?? 0;

            return new CalculateTaxRequest()
            {
                Customer = customer,
                TaxCategoryId = taxCategoryId,
                Address = await GetTaxAddressAsync(customer, product)
            };
        }

        /// <summary>
        /// Checks whether the customer is a consumer (NOT a company) within the EU
        /// </summary>
        /// <param name="customer">Customer to check</param>
        /// <remarks>
        /// A customer is assumed to be an EU consumer if the default tax address does not contain a company name, 
        /// OR the IP address is within the EU, 
        /// OR a business name has been provided but the EU VAT number is invalid
        /// </remarks>
        /// <returns>
        /// <c>True</c> if the customer is a consumer within the EU, <c>False</c> if otherwise
        /// </returns>
        protected virtual bool IsEuConsumer(Customer customer)
        {
            Guard.NotNull(customer, nameof(customer));

            // If BillingAddress is explicitly set but no company is specified, we assume that it is a consumer
            var address = customer.BillingAddress;
            if (address != null && address.Company.IsEmpty())
                return true;
                        
            // Otherwise, check whether customer's IP country is in the EU
            var isInEu = _geoCountryLookup.LookupCountry(customer.LastIpAddress)?.IsInEu is true;
            if (!isInEu)
                return false;

            // Companies with an invalid VAT number are assumed to be consumers
            return customer.VatNumberStatusId != (int)VatNumberStatus.Valid;
        }

        /// <summary>
        /// Gets tax address of customer
        /// </summary>
        /// <param name="customer">Customer of tax address</param>
        /// <param name="product">The related product is used for caching and ESD check. Can be <c>null</c></param>
        /// <remarks>
        /// Tries to get customer address from cached addresses before accessing database
        /// </remarks>
        /// <returns>
        /// Customer's tax address
        /// </returns>
        protected virtual async Task<Address> GetTaxAddressAsync(Customer customer, Product product = null)
        {
            Guard.NotNull(customer, nameof(customer));

            var productIsEsd = product?.IsEsd ?? false;
            var cacheKey = new TaxAddressKey(customer.Id, productIsEsd);

            if (_cachedTaxAddresses.TryGetValue(cacheKey, out var address))
                return address;

            // According to the EU VAT regulations for electronic services from 2015,            
            // VAT must be charged in the EU country from which the customer originates (BILLING address).
            // In addition, the origin of the IP addresses should also be checked for verification.

            var basedOn = _taxSettings.TaxBasedOn;
            if (_taxSettings.EuVatEnabled && productIsEsd && IsEuConsumer(customer))
            {
                basedOn = TaxBasedOn.BillingAddress;
            }

            if (basedOn is TaxBasedOn.BillingAddress && customer?.BillingAddress == null)
            {
                basedOn = TaxBasedOn.DefaultAddress;
            }
            else if (basedOn is TaxBasedOn.ShippingAddress && customer?.ShippingAddress == null)
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

        public virtual Provider<ITaxProvider> LoadActiveTaxProvider()
        {
            var taxProvider = LoadTaxProviderBySystemName(_taxSettings.ActiveTaxProviderSystemName);
            if (taxProvider is null)
            {
                taxProvider = LoadAllTaxProviders().FirstOrDefault();
            }

            return taxProvider;
        }

        public virtual Provider<ITaxProvider> LoadTaxProviderBySystemName(string systemName)
            => _providerManager.GetProvider<ITaxProvider>(systemName);

        public virtual IEnumerable<Provider<ITaxProvider>> LoadAllTaxProviders()
            => _providerManager.GetAllProviders<ITaxProvider>();

        public virtual async Task<decimal> GetTaxRateAsync(Product product, int? taxCategoryId = null, Customer customer = null)
        {
            // No need to calculate anything if price is 0
            if (product?.Price is null or decimal.Zero)
                return decimal.Zero;

            taxCategoryId ??= product?.TaxCategoryId ?? 0;
            customer ??= _workContext.CurrentCustomer;

            var cacheKey = CreateTaxRateCacheKey(customer, taxCategoryId.Value, product);
            if (_cachedTaxRates.TryGetValue(cacheKey, out var taxRate))
                return taxRate;

            var activeTaxProvider = LoadActiveTaxProvider();
            if (activeTaxProvider is null || await IsTaxExemptAsync(product, customer))
                return decimal.Zero;

            var request = await CreateCalculateTaxRequestAsync(customer, taxCategoryId.Value, product);
            var result = await activeTaxProvider.Value.GetTaxRateAsync(request);

            taxRate = result.Success
                ? Math.Max(0, result.TaxRate)
                : decimal.Zero;

            _cachedTaxRates[cacheKey] = taxRate;

            return taxRate;
        }

        public virtual async Task<decimal> GetProductPriceAsync(
            Product product,
            decimal price,
            bool? includingTax = null,
            bool? priceIncludesTax = null,
            int? taxCategoryId = null,
            Currency currency = null,
            Customer customer = null)
        {
            // No need to calculate anything if price is 0
            if (price == decimal.Zero)
                return price;

            customer ??= _workContext.CurrentCustomer;
            currency ??= _workContext.WorkingCurrency;
            taxCategoryId ??= product?.TaxCategoryId;
            var taxRate = await GetTaxRateAsync(product, taxCategoryId, customer);

            var isPriceIncrease = (priceIncludesTax ?? _taxSettings.PricesIncludeTax)
                && (includingTax ?? _workContext.TaxDisplayType == TaxDisplayType.IncludingTax);

            return CalculatePrice(price, taxRate, isPriceIncrease, currency);
        }

        public virtual Task<decimal> GetShippingPriceAsync(
            decimal price,
            bool? includingTax = null,
            Customer customer = null,
            int? taxCategoryId = null)
        {
            if (!_taxSettings.ShippingIsTaxable)
                return Task.FromResult(price);

            taxCategoryId ??= _taxSettings.ShippingTaxClassId;

            return GetProductPriceAsync(
                null,
                price,
                includingTax,
                _taxSettings.ShippingPriceIncludesTax,
                taxCategoryId,
                _workContext.WorkingCurrency,
                customer);
        }

        public virtual Task<decimal> GetPaymentMethodAdditionalFeeAsync(
            decimal price,
            bool? includingTax = null,
            int? taxCategoryId = null,
            Customer customer = null)
        {
            if (!_taxSettings.PaymentMethodAdditionalFeeIsTaxable)
                return Task.FromResult(price);

            taxCategoryId ??= _taxSettings.PaymentMethodAdditionalFeeTaxClassId;

            return GetProductPriceAsync(
                null,
                price,
                includingTax,
                _taxSettings.PaymentMethodAdditionalFeeIncludesTax,
                taxCategoryId,
                _workContext.WorkingCurrency,
                customer);
        }

        public virtual Task<decimal> GetCheckoutAttributePriceAsync(
            CheckoutAttributeValue attributeValue,
            Customer customer = null,
            bool? includingTax = null)
        {
            Guard.NotNull(attributeValue, nameof(attributeValue));
            
            if (attributeValue.CheckoutAttribute.IsTaxExempt)
                return Task.FromResult(attributeValue.PriceAdjustment);

            return GetProductPriceAsync(
                null,
                attributeValue.PriceAdjustment,
                includingTax,
                _taxSettings.PricesIncludeTax,
                attributeValue.CheckoutAttribute.TaxCategoryId,
                _workContext.WorkingCurrency,
                customer);
        }

        public virtual VatNumberStatus GetVatNumberStatus(string fullVatNumber, out string name, out string address)
        {
            name = address = string.Empty;

            if (!fullVatNumber.HasValue())
                return VatNumberStatus.Empty;

            // GB 111 1111 111 or GB 1111111111
            // More advanced regex - https://forum.codeigniter.com/thread-31835.html
            // This regex only checks whether the first two chars are alphanumeric...
            var regex = new Regex(@"^(\w{2})(.*)");
            var match = regex.Match(fullVatNumber.Trim());
            if (!match.Success)
                return VatNumberStatus.Invalid;

            var twoLetterIsoCode = match.Groups[1].Value;
            var vatNumber = match.Groups[2].Value;
            if (!twoLetterIsoCode.HasValue() || !vatNumber.HasValue())
                return VatNumberStatus.Empty;

            vatNumber = vatNumber.Replace(" ", string.Empty);
            twoLetterIsoCode = twoLetterIsoCode.ToUpper();

            return VatNumberStatus.Valid;
            // TODO: (ms) (core) implement EuropeCheckVatService 
            //EuropeCheckVatService.checkVatService vatService = null;
            //try
            //{
            //    vatService = new EuropeCheckVatService.checkVatService();
            //    vatService.checkVat(ref twoLetterIsoCode, ref vatNumber, out var valid, out name, out address);

            //    return valid ? VatNumberStatus.Valid : VatNumberStatus.Invalid;
            //}
            //catch (Exception)
            //{
            //    name = address = string.Empty;
            //    return VatNumberStatus.Unknown;
            //}
            //finally
            //{
            //    name ??= string.Empty;
            //    address ??= string.Empty;

            //    if (vatService != null)
            //    {
            //        vatService.Dispose();
            //    }
            //}
        }

        public virtual async Task<bool> IsTaxExemptAsync(Product product, Customer customer)
        {
            if (customer != null)
            {
                if (customer.IsTaxExempt)
                    return true;

                var hasTaxExemptRole = await _db.CustomerRoles
                    .Where(x => x.Active && x.TaxExempt)
                    .AnyAsync();

                if (hasTaxExemptRole)
                    return true;
            }

            return product?.IsTaxExempt ?? false;
        }

        public virtual async Task<bool> IsVatExemptAsync(Customer customer, Address address = null)
        {
            if (!_taxSettings.EuVatEnabled || customer is null)
                return false;

            address ??= await GetTaxAddressAsync(customer);
            if (address?.Country is null)
                return false;

            // VAT is not charged for shipping outside the VAT zone
            if (!address.Country.SubjectToVat || address.CountryId != _taxSettings.EuVatShopCountryId)
                return true;

            // VAT is not charged if customers VAT status is valid and shop configurations allow VAT exemptions
            var vatStatusIsValid = (VatNumberStatus)customer.VatNumberStatusId is VatNumberStatus.Valid;
            return vatStatusIsValid && _taxSettings.EuVatAllowVatExemption;
        }
    }
}