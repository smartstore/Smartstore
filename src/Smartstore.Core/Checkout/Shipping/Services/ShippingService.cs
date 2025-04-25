using System.Linq.Dynamic.Core;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
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
        private readonly IRequestCache _requestCache;
        private readonly IRoundingHelper _roundingHelper;
        private readonly IStoreContext _storeContext;
        private readonly SmartDbContext _db;

        public ShippingService(
            IProductAttributeMaterializer productAttributeMaterializer,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            IRuleProviderFactory ruleProviderFactory,
            ShippingSettings shippingSettings,
            IProviderManager providerManager,
            ISettingFactory settingFactory,
            IRequestCache requestCache,
            IRoundingHelper roundingHelper,
            IStoreContext storeContext,
            SmartDbContext db)
        {
            _productAttributeMaterializer = productAttributeMaterializer;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            _cartRuleProvider = ruleProviderFactory.GetProvider<ICartRuleProvider>(RuleScope.Cart);
            _shippingSettings = shippingSettings;
            _providerManager = providerManager;
            _settingFactory = settingFactory;
            _requestCache = requestCache;
            _roundingHelper = roundingHelper;
            _storeContext = storeContext;
            _db = db;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public virtual IEnumerable<Provider<IShippingRateComputationMethod>> LoadEnabledShippingProviders(int storeId = 0, string systemName = null)
        {
            var allProviders = _providerManager.GetAllProviders<IShippingRateComputationMethod>(storeId);

            // Get active shipping rate computation methods.
            var enabledProviders = allProviders
                .Where(p => _shippingSettings.ActiveShippingRateComputationMethodSystemNames.Contains(p.Metadata.SystemName, StringComparer.InvariantCultureIgnoreCase));

            if (!enabledProviders.Any())
            {
                // Try get a fallback shipping rate computation method.
                var fallbackProvider = allProviders.FirstOrDefault(x => x.IsShippingProviderEnabled(_shippingSettings)) ?? allProviders.FirstOrDefault();

                if (fallbackProvider != null)
                {
                    _shippingSettings.ActiveShippingRateComputationMethodSystemNames.Clear();
                    _shippingSettings.ActiveShippingRateComputationMethodSystemNames.Add(fallbackProvider.Metadata.SystemName);

                    _settingFactory.SaveSettingsAsync(_shippingSettings).GetAwaiter().GetResult();

                    return [fallbackProvider];
                }

                if (DataSettings.DatabaseIsInstalled())
                {
                    throw new Exception(T("Shipping.OneActiveMethodProviderRequired"));
                }
            }

            return enabledProviders;
        }

        public virtual async Task<List<ShippingMethod>> GetAllShippingMethodsAsync(int storeId = 0, bool matchRules = false)
        {
            var query = _db.ShippingMethods.AsQueryable();

            if (matchRules)
            {
                query = query.Include(x => x.RuleSets);
            }

            var shippingMethods = await query
                .ApplyStoreFilter(storeId)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            if (matchRules)
            {
                void contextAction(CartRuleContext context)
                {
                    if (storeId > 0 && storeId != context.Store.Id)
                    {
                        context.Store = _storeContext.GetStoreById(storeId);
                    }
                }

                return await shippingMethods
                    .WhereAwait(async x => await _cartRuleProvider.RuleMatchesAsync(x, contextAction: contextAction))
                    .AsyncToList();
            }

            return shippingMethods;
        }

        public virtual async Task<decimal> GetCartItemWeightAsync(OrganizedShoppingCartItem cartItem, bool multiplyByQuantity = true)
        {
            Guard.NotNull(cartItem);

            var weight = await GetCartWeight([cartItem], multiplyByQuantity, true);
            return weight;
        }

        public virtual async Task<decimal> GetCartTotalWeightAsync(ShoppingCart cart, bool includeFreeShippingProducts = true)
        {
            Guard.NotNull(cart);

            var cacheKey = $"shipping-cart-total-weight:{cart.GetHashCode()}-{includeFreeShippingProducts}";
            var cartWeight = await _requestCache.GetAsync(cacheKey, async () =>
            {
                var weight = await GetCartWeight(cart.Items, true, includeFreeShippingProducts);

                // Checkout attributes.
                if (cart.Customer != null)
                {
                    var checkoutAttributes = cart.Customer.GenericAttributes.CheckoutAttributes;
                    if (checkoutAttributes.AttributesMap.Any())
                    {
                        var attributeValues = await _checkoutAttributeMaterializer.MaterializeCheckoutAttributeValuesAsync(checkoutAttributes);
                        weight += attributeValues.Sum(x => x.WeightAdjustment);
                    }
                }

                return weight;
            });

            return cartWeight;
        }

        public virtual async Task<ShippingOptionRequest> CreateShippingOptionRequestAsync(
            ShoppingCart cart, 
            Address shippingAddress, 
            int storeId,
            bool matchRules = true)
        {
            return new()
            {
                MatchRules = matchRules,
                StoreId = storeId,
                Customer = cart.Customer,
                ShippingAddress = shippingAddress == null && _shippingSettings.UseShippingOriginIfShippingAddressMissing
                    ? await GetShippingOriginAddressAsync()
                    : shippingAddress,
                CountryFrom = null,
                StateProvinceFrom = null,
                ZipPostalCodeFrom = string.Empty,
                Items = [.. cart.Items.Where(x => x.Item.IsShippingEnabled)]
            };
        }

        public virtual async Task<ShippingOptionResponse> GetShippingOptionsAsync(ShippingOptionRequest request, string allowedShippingRateComputationMethodSystemName = null)
        {
            Guard.NotNull(request);

            var result = new ShippingOptionResponse();
            var computationMethods = LoadEnabledShippingProviders(request.StoreId)
                .Where(x => allowedShippingRateComputationMethodSystemName.IsEmpty() || allowedShippingRateComputationMethodSystemName.EqualsNoCase(x.Metadata.SystemName))
                .ToList();

            if (computationMethods.IsNullOrEmpty())
            {
                throw new InvalidOperationException(T("Shipping.CouldNotLoadMethod"));
            }

            foreach (var method in computationMethods)
            {
                var response = await method.Value.GetShippingOptionsAsync(request);
                foreach (var option in response.ShippingOptions)
                {
                    option.ShippingRateComputationMethodSystemName = method.Metadata.SystemName;
                    option.Rate = _roundingHelper.RoundIfEnabledFor(option.Rate);

                    result.ShippingOptions.Add(option);
                }

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

            var orderedOptions = result.ShippingOptions
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Rate)
                .ThenBy(x => x.ShippingMethodId);

            result.ShippingOptions = [.. orderedOptions];

            // Return valid options if any present (ignores the errors returned by other shipping rate compuation methods).
            if (_shippingSettings.ReturnValidOptionsIfThereAreAny && result.ShippingOptions.Count > 0 && result.Errors.Count > 0)
            {
                result.Errors.Clear();
            }

            // No shipping options loaded.
            if (result.ShippingOptions.Count == 0 && result.Errors.Count == 0)
            {
                result.Errors.Add(T("Checkout.ShippingOptionCouldNotBeLoaded"));
            }

            return result;
        }

        public virtual async Task<Address> GetShippingOriginAddressAsync()
        {
            if (_shippingSettings.ShippingOriginAddressId == 0)
            {
                return null;
            }

            return await _requestCache.GetAsync($"shipping-origin-address-{_shippingSettings.ShippingOriginAddressId}", 
                async () => await _db.Addresses.FindByIdAsync(_shippingSettings.ShippingOriginAddressId, false));
        }

        private async Task<decimal> GetCartWeight(OrganizedShoppingCartItem[] cart, bool multiplyByQuantity, bool includeFreeShippingProducts)
        {
            // INFO: child elements (bundle items) are not taken into account when calculating the weight.
            // Therefore the total weight of a bundle should be specified at the bundled product.

            var cartWeight = decimal.Zero;
            ProductVariantAttributeSelection selection = null;

            // Get all attribute selections in cart.
            if (cart.Length == 0)
            {
                return decimal.Zero;
            }
            else if (cart.Length == 1)
            {
                if (IgnoreCartItem(cart[0].Item))
                {
                    return decimal.Zero;
                }

                selection = cart[0].Item.AttributeSelection;
            }
            else
            {
                selection = new ProductVariantAttributeSelection(string.Empty);
                foreach (var item in cart.Select(x => x.Item))
                {
                    if (!IgnoreCartItem(item))
                    {
                        foreach (var attribute in item.AttributeSelection.AttributesMap)
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

            var linkedProducts = linkedProductIds.Length > 0
                ? await _db.Products.AsNoTracking().Where(x => linkedProductIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id)
                : [];

            // INFO: always iterate cart items (not attribute values). Attributes can occur multiple times in cart.
            foreach (var item in cart.Select(x => x.Item))
            {
                if (IgnoreCartItem(item))
                {
                    continue;
                }

                if (item.AttributeSelection.HasAttributes)
                {
                    await _productAttributeMaterializer.MergeWithCombinationAsync(item.Product, item.AttributeSelection, null);
                }

                var itemWeight = item.Product.Weight;

                // Add attributes weight.
                foreach (var pair in item.AttributeSelection.AttributesMap)
                {
                    var integerValues = pair.Value
                        .Select(x => x.ToString())
                        .Where(x => x.HasValue())
                        .Select(x => x.ToInt())
                        .Where(x => x != 0)
                        .ToArray();

                    foreach (var value in integerValues)
                    {
                        if (attributeValuesDic.TryGetValue(CreateAttributeKey(pair.Key, value), out var attributeValue))
                        {
                            if (attributeValue.ValueType == ProductVariantAttributeValueType.ProductLinkage)
                            {
                                if (linkedProducts.TryGetValue(attributeValue.LinkedProductId, out var linkedProduct) && linkedProduct.IsShippingEnabled)
                                {
                                    itemWeight += linkedProduct.Weight * attributeValue.Quantity;
                                }
                            }
                            else
                            {
                                itemWeight += attributeValue.WeightAdjustment;
                            }
                        }
                    }
                }

                if (multiplyByQuantity)
                {
                    itemWeight *= item.Quantity;
                }

                cartWeight += itemWeight;
            }

            return cartWeight;

            bool IgnoreCartItem(ShoppingCartItem cartItem)
            {
                return !includeFreeShippingProducts && cartItem.Product.IsFreeShipping;
            }

            static string CreateAttributeKey(int productVariantAttributeId, int productVariantAttributeValueId)
            {
                return productVariantAttributeId + "-" + productVariantAttributeValueId;
            }
        }
    }
}