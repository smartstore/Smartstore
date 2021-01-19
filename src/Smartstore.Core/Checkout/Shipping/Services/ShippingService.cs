using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Common;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Domain;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Shipping
{

    public partial class ShippingService : IShippingService
    {
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        //private readonly ICartRuleProvider _cartRuleProvider;
        private readonly ShippingSettings _shippingSettings;
        private readonly IProviderManager _providerManager;
        private readonly ISettingFactory _settingFactory;
        private readonly IWorkContext _workContext;
        private readonly SmartDbContext _db;

        public ShippingService(
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            //ICartRuleProvider cartRuleProvider,
            ShippingSettings shippingSettings,
            IProviderManager providerManager,
            ISettingFactory settingFactory,
            IWorkContext workContext,
            SmartDbContext db)
        {
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            //_cartRuleProvider = cartRuleProvider;
            _shippingSettings = shippingSettings;
            _providerManager = providerManager;
            _settingFactory = settingFactory;
            _workContext = workContext;
            _db = db;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;
        public DbQuerySettings QuerySettings { get; set; } = DbQuerySettings.Default;

        internal async Task<decimal> GetCartItemsAttributesWeightAsync(IList<OrganizedShoppingCartItem> cart, bool multipliedByQuantity = true)
        {
            Guard.NotNull(cart, nameof(cart));

            var rawAttributes = cart
                .Where(x => x.Item.RawAttributes.HasValue())
                .Select(x => x.Item.RawAttributes);

            var selection = new ProductVariantAttributeSelection(string.Empty);
            foreach (var cartItem in cart)
            {
                if (cartItem.Item.RawAttributes.IsEmpty() || cartItem.Item.Product.IsGiftCard)
                    continue;

                var attributeSelection = new ProductVariantAttributeSelection(cartItem.Item.RawAttributes);
                foreach (var attribute in attributeSelection.AttributesMap)
                {
                    if (attribute.Value.IsNullOrEmpty())
                        continue;

                    selection.AddAttribute(attribute.Key, attribute.Value.ToArray());
                }
            }

            var attributeValueIds = selection.GetAttributeValueIds();

            // Gets either all values of attributes without a product linkage
            // or linked products which are shipping enabled
            var query = _db.ProductVariantAttributeValues
                .Include(x => x.ProductVariantAttribute)
                    .ThenInclude(x => x.Product)
                .ApplyValueFilter(attributeValueIds)
                .Where(x => x.ValueTypeId == (int)ProductVariantAttributeValueType.ProductLinkage
                    && x.ProductVariantAttribute.Product != null
                    && x.ProductVariantAttribute.Product.IsShippingEnabled
                    || x.ValueTypeId != (int)ProductVariantAttributeValueType.ProductLinkage);

            // Calculates attributes weight
            // Get attributes without product linkage > add attribute weight adjustment
            var attributesWeight = await query
                .Where(x => x.ValueTypeId != (int)ProductVariantAttributeValueType.ProductLinkage)
                // TODO: (ms) (core) Test possible SumAsync SQL projection failure (IIF)
                .SumAsync(x => x.WeightAdjustment * (multipliedByQuantity ? x.Quantity : 1));

            // TODO: (ms) (core) needs to be tested with NullResult
            // Get attributes with product linkage > add product weigth
            attributesWeight += await query
                .Where(x => x.ValueTypeId == (int)ProductVariantAttributeValueType.ProductLinkage)
                .Select(x => new { x.ProductVariantAttribute.Product, x.Quantity })
                .Where(x => x.Product != null && x.Product.IsShippingEnabled)
                .SumAsync(x => x.Product.Weight * x.Quantity);

            return attributesWeight;
        }

        public virtual IEnumerable<Provider<IShippingRateComputationMethod>> LoadActiveShippingRateComputationMethods(int storeId = 0, string systemName = null)
        {
            var allMethods = _providerManager.GetAllProviders<IShippingRateComputationMethod>(storeId);

            // Get active shipping rate computation methods
            var activeMethods = allMethods
                .Where(p => p.Value.IsActive && _shippingSettings.ActiveShippingRateComputationMethodSystemNames
                .Contains(p.Metadata.SystemName, StringComparer.InvariantCultureIgnoreCase));

            if (activeMethods.Any())
                return activeMethods;

            // Try get a fallback shipping rate computation method
            var fallbackMethod = allMethods.FirstOrDefault(x => x.IsShippingRateComputationMethodActive(_shippingSettings)) ?? allMethods.FirstOrDefault();
            if (fallbackMethod != null)
            {
                _shippingSettings.ActiveShippingRateComputationMethodSystemNames.Clear();
                _shippingSettings.ActiveShippingRateComputationMethodSystemNames.Add(fallbackMethod.Metadata.SystemName);
                _settingFactory.SaveSettingsAsync(_shippingSettings).Await();

                return new Provider<IShippingRateComputationMethod>[] { fallbackMethod };
            }

            if (DataSettings.DatabaseIsInstalled())
                throw new SmartException(T("Shipping.OneActiveMethodProviderRequired"));

            return activeMethods;
        }

        public virtual async Task<List<ShippingMethod>> GetAllShippingMethodsAsync(bool matchRules = false, int storeId = 0)
        {
            var activeShippingMethods = await _db.ShippingMethods
                .ApplyStoreFilter(storeId)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            return activeShippingMethods;
            // TODO: (ms) (core) need cart rules to finish implementation
            //return activeShippingMethods.Where(s =>
            //{
            //    // Rule sets.
            //    if (!_cartRuleProvider.RuleMatches(s))
            //    {
            //        return false;
            //    }

            //    return true;
            //});
        }

        public virtual async Task<decimal> GetCartItemWeightAsync(OrganizedShoppingCartItem cartItem, bool multipliedByQuantity = true)
        {
            Guard.NotNull(cartItem, nameof(cartItem));

            if (cartItem.Item.Product is null)
                return decimal.Zero;

            var attributesWeight = cartItem.Item.RawAttributes.HasValue()
                ? await GetCartItemsAttributesWeightAsync(new List<OrganizedShoppingCartItem> { cartItem }, false)
                : decimal.Zero;

            return multipliedByQuantity
                ? (cartItem.Item.Product.Weight + attributesWeight) * cartItem.Item.Quantity
                : cartItem.Item.Product.Weight + attributesWeight;
        }

        public virtual async Task<decimal> GetCartTotalWeightAsync(IList<OrganizedShoppingCartItem> cart, bool includeFreeShippingProducts = true)
        {
            Guard.NotNull(cart, nameof(cart));

            // Cart total weight > products weight * quantity + attributes weight * quantity
            cart = cart.Where(x => !(!includeFreeShippingProducts && x.Item.Product.IsFreeShipping)).ToList();
            var attributesTotalWeight = await GetCartItemsAttributesWeightAsync(cart);
            var productsTotalWeight = cart.Sum(x => x.Item.Product.Weight * x.Item.Quantity);
            var cartTotalWeight = attributesTotalWeight + productsTotalWeight;

            var customer = cart.GetCustomer();
            if (customer == null)
                return cartTotalWeight;

            // Checkout attributes
            var checkoutAttributesRaw = customer.GenericAttributes.CheckoutAttributes;
            if (checkoutAttributesRaw.HasValue())
            {
                var attributeValues = await _checkoutAttributeMaterializer
                    .MaterializeCheckoutAttributeValuesAsync(new(checkoutAttributesRaw));

                cartTotalWeight += attributeValues.Sum(x => x.WeightAdjustment);
            }

            return cartTotalWeight;
        }

        public virtual ShippingOptionRequest CreateShippingOptionRequest(IList<OrganizedShoppingCartItem> cart, Address shippingAddress, int storeId)
        {
            var shipping = cart.Where(x => x.Item.IsShippingEnabled);
            var request = new ShippingOptionRequest
            {
                StoreId = storeId,
                Customer = cart.GetCustomer(),
                ShippingAddress = shippingAddress,
                CountryFrom = null,
                StateProvinceFrom = null,
                ZipPostalCodeFrom = string.Empty,

                Items = new List<OrganizedShoppingCartItem>(shipping)
            };

            return request;
        }

        public virtual ShippingOptionResponse GetShippingOptions(
            IList<OrganizedShoppingCartItem> cart,
            Address shippingAddress,
            string computationMethodSystemName = "",
            int storeId = 0)
        {
            Guard.NotNull(cart, nameof(cart));

            var computationMethods = LoadActiveShippingRateComputationMethods(storeId)
                .Where(x => computationMethodSystemName.IsEmpty() || computationMethodSystemName == x.Metadata.SystemName)
                .ToList();

            if (computationMethods.IsNullOrEmpty())
                throw new SmartException(T("Shipping.CouldNotLoadMethod"));

            var request = new ShippingOptionRequest
            {
                StoreId = storeId,
                ShippingAddress = shippingAddress,
                Customer = cart.GetCustomer(),
                Items = cart.Where(x => x.Item.IsShippingEnabled).ToList()
            };

            // Get shipping options
            var result = new ShippingOptionResponse();
            foreach (var method in computationMethods)
            {
                var response = method.Value.GetShippingOptions(request);
                foreach (var option in response.ShippingOptions)
                {
                    option.ShippingRateComputationMethodSystemName = method.Metadata.SystemName;
                    option.Rate = _workContext.WorkingCurrency.RoundIfEnabledFor(option.Rate);

                    result.ShippingOptions.Add(option);
                }

                // Log errors
                if (!response.Success)
                {
                    foreach (var error in response.Errors)
                    {
                        result.Errors.Add(error);
                        if (!request.Items.IsNullOrEmpty())
                        {
                            Logger.Warn(error);
                        }
                    }
                }
            }

            // Return valid options if any present (ignores the errors returned by other shipping rate compuation methods)
            if (_shippingSettings.ReturnValidOptionsIfThereAreAny && result.ShippingOptions.Count > 0 && !result.Success)
            {
                result.Errors.Clear();
            }

            // No shipping options loaded
            if (result.ShippingOptions.Count == 0 && result.Success)
            {
                result.Errors.Add(T("Checkout.ShippingOptionCouldNotBeLoaded"));
            }

            return result;
        }
    }
}