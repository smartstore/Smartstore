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
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Common;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Shipping
{
    public partial class ShippingService : IShippingService
    {
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        private readonly ICartRuleProvider _cartRuleProvider;
        private readonly ShippingSettings _shippingSettings;
        private readonly IProviderManager _providerManager;
        private readonly ISettingFactory _settingFactory;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly SmartDbContext _db;

        public ShippingService(
            IProductAttributeMaterializer productAttributeMaterializer,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            ICartRuleProvider cartRuleProvider,
            ShippingSettings shippingSettings,
            IProviderManager providerManager,
            ISettingFactory settingFactory,
            IStoreContext storeContext,
            IWorkContext workContext,
            SmartDbContext db)
        {
            _productAttributeMaterializer = productAttributeMaterializer;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            _cartRuleProvider = cartRuleProvider;
            _shippingSettings = shippingSettings;
            _providerManager = providerManager;
            _settingFactory = settingFactory;
            _storeContext = storeContext;
            _workContext = workContext;
            _db = db;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public virtual IEnumerable<Provider<IShippingRateComputationMethod>> LoadActiveShippingRateComputationMethods(int storeId = 0, string systemName = null)
        {
            var allMethods = _providerManager.GetAllProviders<IShippingRateComputationMethod>(storeId);

            // Get active shipping rate computation methods.
            var activeMethods = allMethods
                .Where(p => p.Value.IsActive && _shippingSettings.ActiveShippingRateComputationMethodSystemNames.Contains(p.Metadata.SystemName, StringComparer.InvariantCultureIgnoreCase));

            if (!activeMethods.Any())
            {
                // Try get a fallback shipping rate computation method.
                var fallbackMethod = allMethods.FirstOrDefault(x => x.IsShippingRateComputationMethodActive(_shippingSettings)) ?? allMethods.FirstOrDefault();

                if (fallbackMethod != null)
                {
                    _shippingSettings.ActiveShippingRateComputationMethodSystemNames.Clear();
                    _shippingSettings.ActiveShippingRateComputationMethodSystemNames.Add(fallbackMethod.Metadata.SystemName);

                    _settingFactory.SaveSettingsAsync(_shippingSettings).GetAwaiter().GetResult();

                    return new Provider<IShippingRateComputationMethod>[] { fallbackMethod };
                }

                if (DataSettings.DatabaseIsInstalled())
                {
                    throw new SmartException(T("Shipping.OneActiveMethodProviderRequired"));
                }
            }

            return activeMethods;
        }

        public virtual async Task<List<ShippingMethod>> GetAllShippingMethodsAsync(int storeId = 0, bool matchRules = false)
        {
            var shippingMethods = await _db.ShippingMethods
                .ApplyStoreFilter(storeId)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            if (matchRules)
            {
                return await shippingMethods
                    .WhereAsync(async x => await _cartRuleProvider.RuleMatchesAsync(x))
                    .AsyncToList();
            }

            return shippingMethods;
        }

        public virtual async Task<decimal> GetCartItemWeightAsync(OrganizedShoppingCartItem cartItem, bool multiplyByQuantity = true)
        {
            Guard.NotNull(cartItem, nameof(cartItem));

            var weight = await GetCartWeight(new List<OrganizedShoppingCartItem> { cartItem }, multiplyByQuantity, true);
            return weight;
        }

        public virtual async Task<decimal> GetCartTotalWeightAsync(IList<OrganizedShoppingCartItem> cart, bool includeFreeShippingProducts = true)
        {
            Guard.NotNull(cart, nameof(cart));

            var customer = cart.GetCustomer();
            var cartWeight = await GetCartWeight(cart, true, includeFreeShippingProducts);

            // Checkout attributes.
            if (customer != null)
            {
                var checkoutAttributes = customer.GenericAttributes.CheckoutAttributes;
                if (checkoutAttributes.AttributesMap.Any())
                {
                    var attributeValues = await _checkoutAttributeMaterializer.MaterializeCheckoutAttributeValuesAsync(checkoutAttributes);

                    cartWeight += attributeValues.Sum(x => x.WeightAdjustment);
                }
            }

            return cartWeight;
        }

        public virtual ShippingOptionRequest CreateShippingOptionRequest(IList<OrganizedShoppingCartItem> cart, Address shippingAddress, int storeId)
        {
            var shippingItems = cart.Where(x => x.Item.IsShippingEnabled);

            var request = new ShippingOptionRequest
            {
                StoreId = storeId,
                Customer = cart.GetCustomer(),
                ShippingAddress = shippingAddress,
                CountryFrom = null,
                StateProvinceFrom = null,
                ZipPostalCodeFrom = string.Empty,

                Items = new List<OrganizedShoppingCartItem>(shippingItems)
            };

            return request;
        }

        public virtual ShippingOptionResponse GetShippingOptions(ShippingOptionRequest request, string allowedShippingRateComputationMethodSystemName = null)
        {
            Guard.NotNull(request, nameof(request));

            var computationMethods = LoadActiveShippingRateComputationMethods(request.StoreId)
                .Where(x => allowedShippingRateComputationMethodSystemName.IsEmpty() || allowedShippingRateComputationMethodSystemName.EqualsNoCase(x.Metadata.SystemName))
                .ToList();

            if (computationMethods.IsNullOrEmpty())
            {
                throw new SmartException(T("Shipping.CouldNotLoadMethod"));
            }

            // Get shipping options.
            var primaryCurrency = _storeContext.CurrentStore.PrimaryStoreCurrency;
            var workingCurrency = _workContext.WorkingCurrency;
            var result = new ShippingOptionResponse();

            foreach (var method in computationMethods)
            {
                var response = method.Value.GetShippingOptions(request);
                foreach (var option in response.ShippingOptions)
                {
                    option.ShippingRateComputationMethodSystemName = method.Metadata.SystemName;
                    option.Rate = new(workingCurrency.RoundIfEnabledFor(option.Rate.Amount), primaryCurrency);

                    result.ShippingOptions.Add(option);
                }

                // Log errors.
                if (!response.Success)
                {
                    foreach (var error in response.Errors)
                    {
                        result.Errors.Add(error);
                        
                        if (request?.Items?.Any() ?? false)
                        {
                            Logger.Warn(error);
                        }
                    }
                }
            }

            // Return valid options if any present (ignores the errors returned by other shipping rate compuation methods).
            if (_shippingSettings.ReturnValidOptionsIfThereAreAny && result.ShippingOptions.Any() && result.Errors.Any())
            {
                result.Errors.Clear();
            }

            // No shipping options loaded.
            if (!result.ShippingOptions.Any() && !result.Errors.Any())
            {
                result.Errors.Add(T("Checkout.ShippingOptionCouldNotBeLoaded"));
            }

            return result;
        }

        private async Task<decimal> GetCartWeight(IList<OrganizedShoppingCartItem> cart, bool multiplyByQuantity, bool includeFreeShippingProducts)
        {
            // INFO: child elements (bundle items) are not taken into account when calculating the weight.
            // Therefore the total weight of a bundle should be specified at the bundled product.

            var numberOfCartItems = cart?.Count ?? 0;
            var cartWeight = decimal.Zero;
            ProductVariantAttributeSelection selection = null;

            // Get all attribute selections in cart.
            if (numberOfCartItems == 0)
            {
                return decimal.Zero;
            }
            else if (numberOfCartItems == 1)
            {
                if (IgnoreCartItem(cart[0]))
                {
                    return decimal.Zero;
                }

                selection = cart[0].Item.AttributeSelection;
            }
            else
            {
                selection = new ProductVariantAttributeSelection(string.Empty);
                foreach (var item in cart)
                {
                    if (!IgnoreCartItem(item))
                    {
                        foreach (var attribute in item.Item.AttributeSelection.AttributesMap)
                        {
                            if (!attribute.Value.IsNullOrEmpty())
                            {
                                selection.AddAttribute(attribute.Key, attribute.Value);
                            }
                        }
                    }
                }
            }

            // Load all attribute values in cart in one go.
            var attributeValues = await _productAttributeMaterializer.MaterializeProductVariantAttributeValuesAsync(selection);
            var attributeValuesDic = attributeValues.ToDictionarySafe(x => CreateAttributeKey(x.ProductVariantAttributeId, x.Id), StringComparer.OrdinalIgnoreCase);

            var linkedProductIds = attributeValues
                .Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage && x.LinkedProductId != 0)
                .Select(x => x.LinkedProductId)
                .Distinct()
                .ToArray();

            var linkedProducts = linkedProductIds.Any()
                ? await _db.Products.AsNoTracking().Where(x => linkedProductIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id)
                : new Dictionary<int, Product>();

            // INFO: always iterate cart items (not attribute values). Attributes can occur multiple times in cart.
            foreach (var item in cart)
            {
                if (IgnoreCartItem(item))
                {
                    continue;
                }

                var itemWeight = item.Item.Product.Weight;

                // Add attributes weight.
                foreach (var pair in item.Item.AttributeSelection.AttributesMap)
                {
                    var numericValues = pair.Value
                        .Select(x => x.ToString())
                        .Where(x => x.HasValue())
                        .Select(x => x.ToInt())
                        .Where(x => x != 0)
                        .ToArray();

                    foreach (var numericValue in numericValues)
                    {
                        if (attributeValuesDic.TryGetValue(CreateAttributeKey(pair.Key, numericValue), out var value))
                        {
                            if (value.ValueType == ProductVariantAttributeValueType.ProductLinkage)
                            {
                                if (linkedProducts.TryGetValue(value.LinkedProductId, out var linkedProduct) && linkedProduct.IsShippingEnabled)
                                {
                                    itemWeight += linkedProduct.Weight * value.Quantity;
                                }
                            }
                            else
                            {
                                itemWeight += value.WeightAdjustment;
                            }
                        }
                    }
                }

                if (multiplyByQuantity)
                {
                    itemWeight *= item.Item.Quantity;
                }

                cartWeight += itemWeight;
            }
            
            return cartWeight;

            bool IgnoreCartItem(OrganizedShoppingCartItem cartItem)
            {
                return !includeFreeShippingProducts && cartItem.Item.Product.IsFreeShipping;
            }

            static string CreateAttributeKey(int productVariantAttributeId, int productVariantAttributeValueId)
            {
                return productVariantAttributeId + "-" + productVariantAttributeValueId;
            }
        }
    }
}