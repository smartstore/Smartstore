using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Engine.Modularity;

namespace Smartstore.Web.Models.Checkout
{
    public static partial class ShoppingCartMappingExtensions
    {
        public static async Task MapAsync(this ShoppingCart cart, CheckoutShippingMethodModel model, dynamic parameters = null)
        {
            await MapperFactory.MapAsync(cart, model, parameters);
        }
    }

    public class CheckoutShippingMethodMapper : Mapper<ShoppingCart, CheckoutShippingMethodModel>
    {
        private readonly ICommonServices _services;
        private readonly IProviderManager _providerManager;
        private readonly ModuleManager _moduleManager;
        private readonly ICurrencyService _currencyService;
        private readonly ITaxService _taxService;
        private readonly IShippingService _shippingService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ITaxCalculator _taxCalculator;

        public CheckoutShippingMethodMapper(
            ICommonServices services,
            IProviderManager providerManager,
            ModuleManager moduleManager,
            ICurrencyService currencyService,
            ITaxService taxService,
            IShippingService shippingService,
            IOrderCalculationService orderCalculationService,
            ITaxCalculator taxCalculator)
        {
            _services = services;
            _providerManager = providerManager;
            _moduleManager = moduleManager;
            _currencyService = currencyService;
            _taxService = taxService;
            _shippingService = shippingService;
            _orderCalculationService = orderCalculationService;
            _taxCalculator = taxCalculator;
        }

        protected override void Map(ShoppingCart from, CheckoutShippingMethodModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(ShoppingCart from, CheckoutShippingMethodModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            var shippingOptionResponse = (parameters?.ShippingOptionResponse as ShippingOptionResponse) ?? new ShippingOptionResponse();
            Guard.NotNull(shippingOptionResponse, nameof(shippingOptionResponse));

            var store = _services.StoreContext.CurrentStore;
            var customer = _services.WorkContext.CurrentCustomer;

            if (shippingOptionResponse.Success)
            {
                // Performance optimization. cache returned shipping options.
                // We'll use them later (after a customer has selected an option).
                customer.GenericAttributes.OfferedShippingOptions = shippingOptionResponse.ShippingOptions;

                var shippingMethods = await _shippingService.GetAllShippingMethodsAsync(store.Id);

                foreach (var shippingOption in shippingOptionResponse.ShippingOptions)
                {
                    var soModel = new CheckoutShippingMethodModel.ShippingMethodModel
                    {
                        ShippingMethodId = shippingOption.ShippingMethodId,
                        Name = shippingOption.Name,
                        Description = shippingOption.Description,
                        ShippingRateComputationMethodSystemName = shippingOption.ShippingRateComputationMethodSystemName,
                    };

                    var srcmProvider = _providerManager.GetProvider<IShippingRateComputationMethod>(shippingOption.ShippingRateComputationMethodSystemName);

                    if (srcmProvider != null)
                    {
                        soModel.BrandUrl = _moduleManager.GetBrandImageUrl(srcmProvider.Metadata);
                    }

                    // Adjust rate.
                    var shippingTaxFormat = _taxService.GetTaxFormat(null, null, PricingTarget.ShippingCharge);
                    var (shippingAmount, _) = await _orderCalculationService.AdjustShippingRateAsync(from, shippingOption.Rate, shippingOption, shippingMethods);
                    var rateBase = await _taxCalculator.CalculateShippingTaxAsync(shippingAmount);
                    var rate = _currencyService.ConvertFromPrimaryCurrency(rateBase.Price, _services.WorkContext.WorkingCurrency);
                    soModel.Fee = rate.WithPostFormat(shippingTaxFormat);

                    to.ShippingMethods.Add(soModel);
                }

                // Find a selected (previously) shipping method.
                var selectedShippingOption = customer.GenericAttributes.SelectedShippingOption;
                if (selectedShippingOption != null)
                {
                    var shippingOptionToSelect = to.ShippingMethods
                        .ToList()
                        .Find(
                            so => so.Name.HasValue() &&
                                  so.Name.EqualsNoCase(selectedShippingOption.Name) &&
                                  so.ShippingRateComputationMethodSystemName.HasValue() &&
                                  so.ShippingRateComputationMethodSystemName.EqualsNoCase(selectedShippingOption.ShippingRateComputationMethodSystemName));

                    if (shippingOptionToSelect != null)
                    {
                        shippingOptionToSelect.Selected = true;
                    }
                }

                // If no option has been selected, let's do it for the first one.
                if (to.ShippingMethods.Where(so => so.Selected).FirstOrDefault() == null)
                {
                    var shippingOptionToSelect = to.ShippingMethods.FirstOrDefault();
                    if (shippingOptionToSelect != null)
                    {
                        shippingOptionToSelect.Selected = true;
                    }
                }
            }
            else
            {
                foreach (var error in shippingOptionResponse.Errors)
                {
                    to.Warnings.Add(error);
                }
            }
        }
    }
}
