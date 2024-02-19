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
    public class CheckoutShippingMethodMapper : Mapper<ShoppingCart, CheckoutShippingMethodModel>
    {
        private readonly IWorkContext _workContext;
        private readonly IProviderManager _providerManager;
        private readonly ModuleManager _moduleManager;
        private readonly ICurrencyService _currencyService;
        private readonly ITaxService _taxService;
        private readonly IShippingService _shippingService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ITaxCalculator _taxCalculator;

        public CheckoutShippingMethodMapper(
            IWorkContext workContext,
            IProviderManager providerManager,
            ModuleManager moduleManager,
            ICurrencyService currencyService,
            ITaxService taxService,
            IShippingService shippingService,
            IOrderCalculationService orderCalculationService,
            ITaxCalculator taxCalculator)
        {
            _workContext = workContext;
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
            Guard.NotNull(from);
            Guard.NotNull(to);

            var customer = from.Customer;
            var options = customer.GenericAttributes.OfferedShippingOptions ??
                (await _shippingService.GetShippingOptionsAsync(from, customer.ShippingAddress, storeId: from.StoreId)).ShippingOptions;

            //var shippingOptionResponse = (parameters?.ShippingOptionResponse as ShippingOptionResponse) ?? new ShippingOptionResponse();
            //Guard.NotNull(shippingOptionResponse);

            //if (shippingOptionResponse.Success)
            if (options.Count > 0)
            {
                // Performance optimization. cache returned shipping options.
                // We'll use them later (after a customer has selected an option).
                //customer.GenericAttributes.OfferedShippingOptions = shippingOptionResponse.ShippingOptions;

                var shippingMethods = await _shippingService.GetAllShippingMethodsAsync(from.StoreId);

                //foreach (var option in shippingOptionResponse.ShippingOptions)
                foreach (var option in options)
                {
                    var model = new CheckoutShippingMethodModel.ShippingMethodModel
                    {
                        ShippingMethodId = option.ShippingMethodId,
                        Name = option.Name,
                        Description = option.Description,
                        ShippingRateComputationMethodSystemName = option.ShippingRateComputationMethodSystemName
                    };

                    var provider = _providerManager.GetProvider<IShippingRateComputationMethod>(option.ShippingRateComputationMethodSystemName);
                    if (provider != null)
                    {
                        model.BrandUrl = _moduleManager.GetBrandImage(provider.Metadata)?.DefaultImageUrl;
                    }

                    // Adjust rate.
                    var shippingTaxFormat = _taxService.GetTaxFormat(null, null, PricingTarget.ShippingCharge);
                    var (shippingAmount, _) = await _orderCalculationService.AdjustShippingRateAsync(from, option.Rate, option, shippingMethods);
                    var rateBase = await _taxCalculator.CalculateShippingTaxAsync(shippingAmount);
                    var rate = _currencyService.ConvertFromPrimaryCurrency(rateBase.Price, _workContext.WorkingCurrency);
                    model.Fee = rate.WithPostFormat(shippingTaxFormat);

                    to.ShippingMethods.Add(model);
                }

                // Find a selected (previously) shipping method.
                var selectedShippingOption = customer.GenericAttributes.SelectedShippingOption;
                if (selectedShippingOption != null)
                {
                    var shippingOptionToSelect = to.ShippingMethods
                        .ToList()
                        .Find(so => so.Name.HasValue() &&
                            so.Name.EqualsNoCase(selectedShippingOption.Name) &&
                            so.ShippingRateComputationMethodSystemName.HasValue() &&
                            so.ShippingRateComputationMethodSystemName.EqualsNoCase(selectedShippingOption.ShippingRateComputationMethodSystemName));

                    if (shippingOptionToSelect != null)
                    {
                        shippingOptionToSelect.Selected = true;
                    }
                }

                // If no option has been selected, let's do it for the first one.
                if (to.ShippingMethods.FirstOrDefault(so => so.Selected) == null)
                {
                    var shippingOptionToSelect = to.ShippingMethods.FirstOrDefault();
                    if (shippingOptionToSelect != null)
                    {
                        shippingOptionToSelect.Selected = true;
                    }
                }
            }
            //else
            //{
            //    shippingOptionResponse.Errors.Each(to.Warnings.Add);
            //}
        }
    }
}
