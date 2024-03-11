using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Http;
using Smartstore.Shipping.Settings;
using Smartstore.Utilities;

namespace Smartstore.Shipping
{
    [SystemName("Shipping.ByTotal")]
    [FriendlyName("Shipping by total")]
    [Order(0)]
    internal class ByTotalProvider : IShippingRateComputationMethod, IConfigurable
    {
        private readonly SmartDbContext _db;
        private readonly IRoundingHelper _roundingHelper;
        private readonly IShippingService _shippingService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductService _productService;
        private readonly ShippingByTotalSettings _shippingByTotalSettings;

        public ByTotalProvider(
            SmartDbContext db,
            IRoundingHelper roundingHelper,
            IShippingService shippingService,
            IPriceCalculationService priceCalculationService,
            IProductService productService,
            ShippingByTotalSettings shippingByTotalSettings)
        {
            _db = db;
            _roundingHelper = roundingHelper;
            _shippingService = shippingService;
            _priceCalculationService = priceCalculationService;
            _productService = productService;
            _shippingByTotalSettings = shippingByTotalSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        /// <summary>
        /// Gets the rate for the shipping method
        /// </summary>
        /// <param name="subtotal">the order's subtotal</param>
        /// <param name="shippingMethodId">the shipping method identifier</param>
        /// <param name="countryId">country identifier</param>
        /// <param name="stateProvinceId">state province identifier</param>
        /// <param name="zip">Zip code</param>
        /// <returns>the rate for the shipping method</returns>
		private async Task<decimal?> GetRateAsync(decimal subtotal, int shippingMethodId, int storeId, int countryId, int stateProvinceId, string zip)
        {
            decimal? shippingTotal = null;

            zip = zip.EmptyNull().Trim();

            var shippingByTotalRecords = await _db.ShippingRatesByTotal()
                .Where(x => x.StoreId == storeId || x.StoreId == 0)
                .Where(x => x.ShippingMethodId == shippingMethodId)
                .ApplySubTotalFilter(subtotal)
                .ApplyRegionFilter(countryId, stateProvinceId)
                .ToListAsync();

            var shippingByTotalRecord = shippingByTotalRecords
                .Where(x => (zip.IsEmpty() && x.Zip.IsEmpty()) || ZipMatches(zip, x.Zip))
                .LastOrDefault();

            if (shippingByTotalRecord == null)
            {
                return _shippingByTotalSettings.LimitMethodsToCreated ? null : decimal.Zero;
            }

            decimal baseCharge = shippingByTotalRecord.BaseCharge;
            decimal? maxCharge = shippingByTotalRecord.MaxCharge;

            if (shippingByTotalRecord.UsePercentage && shippingByTotalRecord.ShippingChargePercentage <= decimal.Zero)
            {
                return baseCharge; //decimal.Zero;
            }

            if (!shippingByTotalRecord.UsePercentage && shippingByTotalRecord.ShippingChargeAmount <= decimal.Zero)
            {
                return decimal.Zero;
            }

            if (shippingByTotalRecord.UsePercentage)
            {
                shippingTotal = _roundingHelper.RoundIfEnabledFor((decimal)(((float)subtotal) * ((float)shippingByTotalRecord.ShippingChargePercentage) / 100f));
                shippingTotal += baseCharge;
                if (maxCharge.HasValue && shippingTotal > maxCharge)
                {
                    // Shipping charge should not exceed MaxCharge.
                    shippingTotal = Math.Min(shippingTotal.Value, maxCharge.Value);
                }
            }
            else
            {
                shippingTotal = shippingByTotalRecord.ShippingChargeAmount;
            }

            if (shippingTotal < decimal.Zero)
            {
                shippingTotal = decimal.Zero;
            }

            return shippingTotal;
        }

        private static bool ZipMatches(string zip, string pattern)
        {
            if (pattern.IsEmpty() || pattern == "*")
            {
                return true; // catch all
            }

            var patterns = pattern.Contains(',')
                ? pattern.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim())
                : new string[] { pattern };

            try
            {
                foreach (var entry in patterns)
                {
                    var wildcard = new Wildcard(entry, true);
                    if (wildcard.IsMatch(zip))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return zip.EqualsNoCase(pattern);
            }

            return false;
        }

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "ByTotal", new { area = "Admin" });

        public Task<decimal?> GetFixedRateAsync(ShippingOptionRequest request)
            => Task.FromResult<decimal?>(null);

        public async Task<ShippingOptionResponse> GetShippingOptionsAsync(ShippingOptionRequest request)
        {
            Guard.NotNull(request);

            var response = new ShippingOptionResponse();

            if (request.Items.IsNullOrEmpty())
            {
                response.Errors.Add(T("Admin.System.Warnings.NoShipmentItems"));
                return response;
            }

            int countryId = 0;
            int stateProvinceId = 0;
            string zip = null;
            decimal subTotal = decimal.Zero;

            if (request.ShippingAddress != null)
            {
                countryId = request.ShippingAddress.CountryId ?? 0;
                stateProvinceId = request.ShippingAddress.StateProvinceId ?? 0;
                zip = request.ShippingAddress.ZipPostalCode;
            }

            var allProducts = request.Items
                .Select(x => x.Item.Product)
                .Union(request.Items.Select(x => x.ChildItems).SelectMany(child => child.Select(x => x.Item.Product)))
                .ToArray();

            var store = await _db.Stores.FindByIdAsync(request.StoreId, false);
            var batchContext = _productService.CreateProductBatchContext(allProducts, store, request.Customer, false);
            var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, request.Customer, null, batchContext);
            calculationOptions.IgnoreDiscounts = false;
            calculationOptions.TaxInclusive = _shippingByTotalSettings.CalculateTotalIncludingTax;

            foreach (var shoppingCartItem in request.Items)
            {
                if (shoppingCartItem.Item.IsFreeShipping || !shoppingCartItem.Item.IsShippingEnabled)
                {
                    continue;
                }

                var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(shoppingCartItem, calculationOptions);
                var (unitPrice, itemSubtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);

                subTotal += _roundingHelper.Round(itemSubtotal.FinalPrice);
            }

            var sqThreshold = _shippingByTotalSettings.SmallQuantityThreshold;
            var sqSurcharge = _shippingByTotalSettings.SmallQuantitySurcharge;

            var shippingMethods = await _shippingService.GetAllShippingMethodsAsync(request.StoreId, request.MatchRules);
            foreach (var shippingMethod in shippingMethods)
            {
                decimal? rate = await GetRateAsync(subTotal, shippingMethod.Id, request.StoreId, countryId, stateProvinceId, zip);
                if (rate.HasValue)
                {
                    if (rate > 0 && sqThreshold > 0 && subTotal <= sqThreshold)
                    {
                        // Add small quantity surcharge (Mindermengenzuschlag).
                        rate += sqSurcharge;
                    }

                    var shippingOption = new ShippingOption
                    {
                        ShippingMethodId = shippingMethod.Id,
                        Name = shippingMethod.GetLocalized(x => x.Name),
                        Description = shippingMethod.GetLocalized(x => x.Description),
                        Rate = rate.Value
                    };

                    response.ShippingOptions.Add(shippingOption);
                }
            }

            return response;
        }

        /// <summary>
        /// Gets a shipping rate computation method type
        /// </summary>
        public ShippingRateComputationMethodType ShippingRateComputationMethodType => ShippingRateComputationMethodType.Offline;

        /// <summary>
        /// Gets a shipment tracker
        /// </summary>
        public IShipmentTracker ShipmentTracker =>
                //uncomment the line below to return a general shipment tracker (finds an appropriate tracker by tracking number)
                //return new GeneralShipmentTracker(EngineContext.Current.Resolve<ITypeFinder>());
                null;
    }
}
