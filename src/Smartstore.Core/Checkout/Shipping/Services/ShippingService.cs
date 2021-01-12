using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Configuration;
using Smartstore.Core.Customers;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Data;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Shipping
{
    public partial class ShippingService : IShippingService
    {
        private readonly ICheckoutAttributeMaterializer _attributeMaterializer;
        private readonly IGenericAttributeService _attributeService;
        //private readonly ICartRuleProvider _cartRuleProvider;
        private readonly ShippingSettings _shippingSettings;
        private readonly IProviderManager _providerManager;
        private readonly ISettingFactory _settingFactory;
        private readonly IWorkContext _workContext;
        private readonly SmartDbContext _db;

        public ShippingService(
            ICheckoutAttributeMaterializer attributeMaterializer,
            IGenericAttributeService attributeService,
            //ICartRuleProvider cartRuleProvider,
            ShippingSettings shippingSettings,
            IProviderManager providerManager,
            IProductService productService,
            ISettingService settingService,
            ISettingFactory settingFactory,
            IWorkContext workContext,
            SmartDbContext db)
        {
            _attributeMaterializer = attributeMaterializer;
            //_cartRuleProvider = cartRuleProvider;
            _attributeService = attributeService;
            _shippingSettings = shippingSettings;
            _providerManager = providerManager;
            _settingFactory = settingFactory;
            _workContext = workContext;
            _db = db;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;
        public DbQuerySettings QuerySettings { get; set; } = DbQuerySettings.Default;

        public virtual async Task<IEnumerable<Provider<IShippingRateComputationMethod>>> LoadActiveShippingRateComputationMethodsAsync(int storeId = 0, string systemName = null)
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
                await _settingFactory.SaveSettingsAsync(_shippingSettings);

                var providerList = new Provider<IShippingRateComputationMethod>[] { fallbackMethod };
                return providerList;
            }

            if (DataSettings.DatabaseIsInstalled())
                throw new SmartException(T("Shipping.OneActiveMethodProviderRequired"));

            return activeMethods;
        }

        public virtual Task<List<ShippingMethod>> GetAllShippingMethodsAsync(bool matchRules = false, int storeId = 0)
        {
            var query = _db.ShippingMethods.Select(x => x);

            if (!QuerySettings.IgnoreMultiStore && storeId > 0)
            {
                // Apply store mapping, with linq?
                query = 
                    from x in query
                    join sm in _db.StoreMappings.Select(x => x)
                    on new { c1 = x.Id, c2 = "ShippingMethod" } equals new { c1 = sm.EntityId, c2 = sm.EntityName } into x_sm
                    from sm in x_sm.DefaultIfEmpty()
                    where !x.LimitedToStores || storeId == sm.StoreId
                    select x;

                query =
                    from x in query
                    group x by x.Id into grp
                    orderby grp.Key
                    select grp.FirstOrDefault();
            }

            // TODO: (ms) (core) needs CartRuleProvider
            //if (matchRules)
            //{
            //    return query
            //        .Where(x => _cartRuleProvider.RuleMatches(x))
            //        .OrderBy(x => x.DisplayOrder)
            //        .ToListAsync();
            //}

            return query.OrderBy(x => x.DisplayOrder).ToListAsync();
        }

        public virtual async Task<decimal> GetCartItemWeightAsync(OrganizedShoppingCartItem cartItem, bool multipliedByQuantity = false)
        {
            Guard.NotNull(cartItem, nameof(cartItem));

            if (cartItem.Item.Product is null)
                return decimal.Zero;

            var attributesWeight = decimal.Zero;
            if (cartItem.Item.AttributesXml.HasValue())
            {
                // Gets parsed product variant attribute selection from raw attributes string.
                // Calculates attributes weight
                var selection = new ProductVariantAttributeSelection(cartItem.Item.AttributesXml);
                var ids = selection.AttributesMap.Select(x => x.Key).ToArray();
                var query = _db.ProductVariantAttributeValues.ApplyValueFilter(ids);

                attributesWeight += await query
                    .Where(x => x.ValueType != ProductVariantAttributeValueType.ProductLinkage)
                    .SumAsync(x => x.WeightAdjustment);

                attributesWeight += await query
                    .Include(x => x.ProductVariantAttribute)
                        .ThenInclude(x => x.Product)
                    .Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage)
                    .Select(x => new { x.ProductVariantAttribute.Product, x.Quantity })
                    .Where(x => x.Product != null && x.Product.IsShippingEnabled)
                    .SumAsync(x => x.Product.Weight * x.Quantity);
            }

            return multipliedByQuantity 
                ? (cartItem.Item.Product.Weight + attributesWeight) * cartItem.Item.Quantity
                : cartItem.Item.Product.Weight + attributesWeight;
        }

        public virtual async Task<decimal> GetShoppingCartTotalWeightAsync(IList<OrganizedShoppingCartItem> cart, bool includeFreeShippingProducts = true)
        {
            Guard.NotNull(cart, nameof(cart));

            var totalWeight = (await Task.WhenAll(cart
                .Where(x => !(!includeFreeShippingProducts && x.Item.Product.IsFreeShipping))
                .Select(async x => await GetCartItemWeightAsync(x, true))))
                .Sum();

            var customer = cart.GetCustomer();
            if (customer == null)
                return totalWeight;

            // Checkout attributes
            var checkoutAttributesRaw = _attributeService.GetAttributesForEntity(customer).Map
                .Where(x => x.Key == SystemCustomerAttributeNames.CheckoutAttributes)
                .Select(x => x.Value)
                .FirstOrDefault()
                .ToString();

            if (checkoutAttributesRaw.HasValue())
            {
                var attributeValues = await _attributeMaterializer
                    .MaterializeCheckoutAttributeValuesAsync(new CheckoutAttributeSelection(checkoutAttributesRaw));

                totalWeight += attributeValues.Sum(x => x.WeightAdjustment);
            }

            return totalWeight;
        }

        public virtual async Task<GetShippingOptionResponse> GetShippingOptionsAsync(
            IList<OrganizedShoppingCartItem> cart,
            Address shippingAddress,
            string computationMethodSystemName = "",
            int storeId = 0)
        {
            Guard.NotNull(cart, nameof(cart));

            var computationMethods = (await LoadActiveShippingRateComputationMethodsAsync(storeId))
                .Where(x => computationMethodSystemName.IsEmpty() || computationMethodSystemName == x.Metadata.SystemName)
                .ToList();

            if (computationMethods.IsNullOrEmpty())
                throw new SmartException(T("Shipping.CouldNotLoadMethod"));

            var request = new GetShippingOptionRequest
            {
                StoreId = storeId,
                ShippingAddress = shippingAddress,
                Customer = cart.GetCustomer(),
                Items = cart.Where(x => x.Item.IsShippingEnabled).ToList()
            };

            // Get shipping options
            var result = new GetShippingOptionResponse();
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
                        result.AddError(error);
                        if (!request.Items.IsNullOrEmpty())
                        {
                            Logger.Warn(error);
                        }
                    }
                }
            }

            // Return valid options if any present (ignores the errors returned by other shipping rate compuation methods)
            if (_shippingSettings.ReturnValidOptionsIfThereAreAny && result.ShippingOptions.Count > 0 && result.Errors.Count > 0)
            {
                result.Errors.Clear();
            }

            // No shipping options loaded
            if (result.ShippingOptions.Count == 0 && result.Errors.Count == 0)
            {
                result.Errors.Add(T("Checkout.ShippingOptionCouldNotBeLoaded"));
            }

            return result;
        }
    }
}